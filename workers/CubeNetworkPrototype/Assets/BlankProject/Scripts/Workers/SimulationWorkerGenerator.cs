using BlankProject;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Worker.CInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SimulationWorkerGenerator : WorkerConnector
{
    [SerializeField] int maxSimulatedPlayers = 1;
    [SerializeField] float simuPlayerCreationInterval = 0.1f; // in seconds
    [SerializeField] private float compUpdateFrequence = 1.0f;
    [SerializeField] GameObject simulatedClientWorker = null;
    [SerializeField] private string connectToLanIp = null;
    [SerializeField] private bool useSessionFlow = false;

    private const string flagDevAuthTokenId = "pure_network_simulated_players_dev_auth_token_id";
    private const string flagTargetDeployment = "pure_network_simulated_players_target_deployment";
    private const string flagClientCount = "simulated_players_per_coordinator";
    private const string flagCreationInterval = "simulated_players_creation_interval";

    private const string DeploymentNameFlag = "deploymentName";

    private bool connectPlayersWithDevAuth;

    private CancellationTokenSource tokenSource;
    private delegate void UpdateCoordinatorParams(ref int count, ref float interval);
    private int timeBetweenCycleSecs = 2;

    private readonly List<GameObject> simulatedPlayerWorkers = new List<GameObject>();

    private Rect areaRect = new Rect(350, 0, 300f, 110f);
    private bool isOpen = false;

    private string strLastPlayers = "";
    private string strInterval = "";
    private string strUpdateFrequence = "";

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    private async void Start()
    {
        if (simulatedClientWorker == null)
        {
            Debug.LogError("Plesse specify the SimulatedClientWorker");
        }

        strLastPlayers = maxSimulatedPlayers.ToString();
        strInterval = simuPlayerCreationInterval.ToString();
        strUpdateFrequence = compUpdateFrequence.ToString();

        var args = CommandLineArgs.FromCommandLine();

        var deploymentName = string.Empty;

        if (args.TryGetCommandLineValue(DeploymentNameFlag, ref deploymentName))
        {
            connectPlayersWithDevAuth = deploymentName != "local";
        }
        else
        {
            // We are probably in the Editor.
            connectPlayersWithDevAuth = false;
        }

        await TryConnect();
    }

    private async Task TryConnect()
    {
        var connParams = CreateConnectionParameters(WorkerUtils.SimulatedPlayerCoordinator);
        connParams.Network.ConnectionType = NetworkConnectionType.Kcp;

        if (!string.IsNullOrEmpty(connectToLanIp))
        {
            connParams.Network.UseExternalIp = true;
        }

        var builder = new SpatialOSConnectionHandlerBuilder()
            .SetConnectionParameters(connParams);

        if (!Application.isEditor)
        {
            var initializer = new CommandLineConnectionFlowInitializer();
            switch (initializer.GetConnectionService())
            {
                case ConnectionService.Receptionist:
                    var receptionistFlow = new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.SimulatedPlayerCoordinator), initializer);
                    if (!string.IsNullOrEmpty(connectToLanIp))
                    {
                        receptionistFlow.ReceptionistHost = connectToLanIp;
                    }
                    builder.SetConnectionFlow(receptionistFlow);
                    break;
                case ConnectionService.Locator:
                    builder.SetConnectionFlow(new LocatorFlow(initializer));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            var receptionistFlow = new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.SimulatedPlayerCoordinator));
            if (!string.IsNullOrEmpty(connectToLanIp))
            {
                receptionistFlow.ReceptionistHost = connectToLanIp;
            }
            builder.SetConnectionFlow(receptionistFlow);
        }

        await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
    }

    public override void Dispose()
    {
        tokenSource?.Cancel();
        tokenSource?.Dispose();
        tokenSource = null;

        base.Dispose();
    }

    protected override async void HandleWorkerConnectionEstablished()
    {
        Worker.LogDispatcher.HandleLog(LogType.Log,
            new LogEvent($"SimulationWorkerGenerator.HandleWorkerConnectionEstablished useSessionFlow:{useSessionFlow}"));

        base.HandleWorkerConnectionEstablished();

        Worker.World.GetOrCreateSystem<MetricSendSystem>();

        if (simulatedClientWorker == null)
        {
            return;
        }

        tokenSource = new CancellationTokenSource();

        try
        {
            if (useSessionFlow)
            {
                await WaitForWorkerFlags(flagClientCount, flagCreationInterval);

                int.TryParse(Worker.GetWorkerFlag(flagCreationInterval), out var originalInterval);
                int.TryParse(Worker.GetWorkerFlag(flagClientCount), out var originalCount);

                var workerFlagCallbackSystem = Worker.World.GetExistingSystem<WorkerFlagCallbackSystem>();
                using( var workerFlagTracker = new WorkerFlagTracker(Worker, workerFlagCallbackSystem) )
                {
                    await Monitor(originalCount, originalInterval,
                        connetor => connetor.TryConnect(connectToLanIp),
                        (ref int count, ref float interval) => {
                        MonitorWorkerFlags(workerFlagTracker, ref count, ref interval);
                    });
                }
            }
            else if (connectPlayersWithDevAuth)
            {
                await WaitForWorkerFlags(flagDevAuthTokenId, flagTargetDeployment, flagClientCount,
                        flagCreationInterval);

                var simulatedPlayerDevAuthTokenId = Worker.GetWorkerFlag(flagDevAuthTokenId);
                var simulatedPlayerTargetDeployment = Worker.GetWorkerFlag(flagTargetDeployment);

                int.TryParse(Worker.GetWorkerFlag(flagCreationInterval), out var originalInterval);
                int.TryParse(Worker.GetWorkerFlag(flagClientCount), out var originalCount);

                Worker.LogDispatcher.HandleLog(LogType.Log,
                    new LogEvent($"SimulationWorkerGenerator.HandleWorkerConnectionEstablished " +
                    $" connectPlayersWithDevAuth: {connectPlayersWithDevAuth}\n" +
                    $" targetDeployment:{simulatedPlayerTargetDeployment}\n" +
                    $" devAuthToken:{simulatedPlayerDevAuthTokenId}"));

                var workerFlagCallbackSystem = Worker.World.GetExistingSystem<WorkerFlagCallbackSystem>();
                using (var workerFlagTracker = new WorkerFlagTracker(Worker, workerFlagCallbackSystem))
                {
                    await Monitor(originalCount, originalInterval,
                        connector =>
                            connector.TryConnect(simulatedPlayerDevAuthTokenId,
                                simulatedPlayerTargetDeployment, connectToLanIp),
                        (ref int count, ref float interval) => {
                            MonitorWorkerFlags(workerFlagTracker, ref count, ref interval);
                    });
                }
            }
            else
            {
                await Monitor(maxSimulatedPlayers, simuPlayerCreationInterval,
                    connector => connector.TryConnect(connectToLanIp),
                    (ref int count, ref float interval) => {
                    count = maxSimulatedPlayers;
                    interval = simuPlayerCreationInterval;
                });
            }
        }
        catch(TaskCanceledException)
        {
            // This is fine. Means we have triggered a cancel via Dispose().
        }
    }

    private async Task WaitForWorkerFlags(params string[] flagKeys)
    {
        var token = tokenSource.Token;

        while (flagKeys.Any(key => string.IsNullOrEmpty(Worker.GetWorkerFlag(key))))
        {
            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            Worker.LogDispatcher.HandleLog(LogType.Log, new LogEvent("Waiting for required worker flags.."));
            await Task.Delay(TimeSpan.FromSeconds(timeBetweenCycleSecs), token);
        }
    }

    private void MonitorWorkerFlags(WorkerFlagTracker tracker, ref int count, ref float interval)
    {
        if (tracker.TryGetFlagChange(flagCreationInterval, out var intervalStr) &&
            int.TryParse(intervalStr, out var newInterval))
        {
            interval = newInterval;
        }

        if (tracker.TryGetFlagChange(flagClientCount, out var newCountStr) &&
            int.TryParse(newCountStr, out var newCount))
        {
            count = newCount;
        }

        tracker.Reset();
    }

    private async Task Monitor(int maxPlayers, float interval,
        Func<SimulatedClientConnector, Task> connectMethod,
        UpdateCoordinatorParams updateParams)
    {
        var token = tokenSource.Token;

        Worker.LogDispatcher.HandleLog(LogType.Log,
            new LogEvent($"SimulationWorkerGenerator.Monitor maxPlayers:{maxPlayers}, interval:{interval}"));

        while( Worker != null && Worker.IsConnected )
        {
            if ( token.IsCancellationRequested )
            {
                throw new TaskCanceledException();
            }

            updateParams(ref maxPlayers, ref interval);

            while(simulatedPlayerWorkers.Count < maxPlayers)
            {
                await CreateSimPlayer(interval, connectMethod, token);
            }

            while(simulatedPlayerWorkers.Count > maxPlayers)
            {
                RemoveSimPlayer();
            }

            await Task.Delay(TimeSpan.FromSeconds(timeBetweenCycleSecs), token);
        }
    }

    private void RemoveSimPlayer()
    {
        Worker.LogDispatcher.HandleLog(LogType.Log,
            new LogEvent($"SimulationWorkerGenerator.RemoveSimPlayer"));

        var simWorker = simulatedPlayerWorkers[0];
        simulatedPlayerWorkers.Remove(simWorker);
        Destroy(simWorker);
    }

    private async Task CreateSimPlayer(float interval,
        Func<SimulatedClientConnector, Task> connectMethod,
        CancellationToken token)
    {
        await Task.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(interval, 1.25f * interval)), token);

        Worker.LogDispatcher.HandleLog(LogType.Log,
            new LogEvent($"SimulationWorkerGenerator.CreateSimPlayer:{simulatedPlayerWorkers.Count}"));

        var simWorker = GameObject.Instantiate(simulatedClientWorker, transform);
        var simWorkerConnector = simWorker.GetComponent<SimulatedClientConnector>();

        Worker.LogDispatcher.HandleLog(LogType.Log,
            new LogEvent($"SimulationWorkerGenerator.TryConnect connectToLanIp:{connectToLanIp}"));
        //await simWorkerConnector.TryConnect(connectToLanIp);
        await connectMethod(simWorkerConnector);

        Worker.LogDispatcher.HandleLog(LogType.Log,
            new LogEvent($"SimulationWorkerGenerator.SpawnPlayer SimPlayer_{simulatedPlayerWorkers.Count}"));
        simWorkerConnector.SpawnPlayer(simulatedPlayerWorkers.Count);

        simulatedPlayerWorkers.Add(simWorker);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isOpen = !isOpen;
            if ( isOpen )
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void OnGUI()
    {
        if (!isOpen)
        {
            return;
        }

        GUILayout.BeginArea(areaRect, GUI.skin.box);
        {
            GUILayout.Label("Simu Client Input:");
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("SimPlayers", GUILayout.Width(135f));
                    string strPlayers = null;
                    strPlayers = GUILayout.TextField(strLastPlayers);
                    if (strLastPlayers != strPlayers)
                    {
                        strLastPlayers = strPlayers;
                        if (int.TryParse(strPlayers, out var nplayers))
                        {
                            maxSimulatedPlayers = nplayers;
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Interval", GUILayout.Width(135f));
                    string strIntv = null;
                    strIntv = GUILayout.TextField(strInterval);
                    if (strInterval != strIntv)
                    {
                        strInterval = strIntv;
                        if (float.TryParse(strIntv, out var fIntv))
                        {
                            simuPlayerCreationInterval = fIntv;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndArea();
    }

    private class WorkerFlagTracker : IDisposable
    {
        private readonly Dictionary<string, string> flagChanges = new Dictionary<string, string>();

        private readonly WorkerFlagCallbackSystem callbackSystem;
        private readonly ulong callbackKey;
        private readonly WorkerInWorld worker;

        public WorkerFlagTracker(WorkerInWorld worker, WorkerFlagCallbackSystem callbackSystem)
        {
            this.worker = worker;
            this.callbackSystem = callbackSystem;
            callbackKey = callbackSystem.RegisterWorkerFlagChangeCallback(OnWorkerFlagChange);
        }

        public bool TryGetFlagChange(string key, out string value)
        {
            return flagChanges.TryGetValue(key, out value);
        }

        public void Reset()
        {
            flagChanges.Clear();
        }

        private void OnWorkerFlagChange((string, string) pair)
        {
            worker.LogDispatcher.HandleLog(LogType.Log,
                new LogEvent($"WorkerFlagTracker.OnWorkerFlagChange {pair.Item1}:{pair.Item2}"));

            flagChanges[pair.Item1] = pair.Item2;
        }

        public void Dispose()
        {
            callbackSystem.UnregisterWorkerFlagChangeCallback(callbackKey);
        }
    }
}
