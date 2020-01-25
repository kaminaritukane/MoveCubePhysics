using BlankProject;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.Subscriptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


public class ServerMetrics : TransformUpdateMetrics
{
    private static ServerMetrics instance;
    public static ServerMetrics Instance
    {
        get
        {
            return instance;
        }
    }

    public WorkerSystem workerSystem;
    public CommandSystem commandSystem;

    private Rect areaRect = new Rect(660, 140, 300f, 140f);
    private bool isOpen = false;

    private delegate void UpdateMoveCubeParams(ref int count);

    private string strMaxMoveCubes;
    private int nMaxMoveCubes = -1;

    private CancellationTokenSource tokenSource;
    private bool isSpawningOrRemoving = false;

    private ServerGameObjectCreator creator;

    private void Awake()
    {
        instance = this;
    }

    private async void Start()
    {
        strMaxMoveCubes = nMaxMoveCubes.ToString();

        var connector = GetComponent<UnityGameLogicConnector>();
        if ( connector != null )
        {
            connector.OnCreatorCreated += Connector_OnCreatorCreated;
        }

        tokenSource = new CancellationTokenSource();

        try
        {
            await Monitor(nMaxMoveCubes,
                (ref int maxPlayers) =>
                {
                    maxPlayers = nMaxMoveCubes;
                }
            );
        }
        catch (TaskCanceledException)
        {

        }
    }

    private async Task Monitor(int maxCubes,
        UpdateMoveCubeParams updateParams)
    {
        var token = tokenSource.Token;

        while (true)
        {
            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            if (isSpawningOrRemoving)
            {
                var nMoveCubes = creator.GetEntityCubeAmount();
                if (nMoveCubes == maxCubes)
                {
                    isSpawningOrRemoving = false;
                }
                else
                {
                    await Task.Delay(2000, token);
                    continue;
                }
            }

            if (creator != null)
            {
                updateParams(ref maxCubes);

                if (maxCubes >= 0)
                {
                    var nMoveCubes = creator.GetEntityCubeAmount();
                    if (nMoveCubes < maxCubes)
                    {
                        var spawnCount = maxCubes - nMoveCubes;
                        SpawnMoveCubes(spawnCount, token);
                        isSpawningOrRemoving = true;
                    }
                    else if (nMoveCubes > maxCubes)
                    {
                        var delta = nMoveCubes - maxCubes;
                        DeleteMoveCubes(delta);
                        isSpawningOrRemoving = true;
                    }
                }
            }

            await Task.Delay(5000, token);
        }
    }

    private void SpawnMoveCubes(int count, CancellationToken token)
    {
        var cubeName = $"MoveCube_";

        for (int i = 0; i < count; ++i)
        {
            var moveCubeTemp = EntityTemplates.CreateMoveCubeEntityTemplate(
                workerSystem.WorkerId,
                cubeName
            );
            commandSystem.SendCommand(new WorldCommands.CreateEntity.Request(moveCubeTemp));
        }
    }

    private void DeleteMoveCubes(int amount)
    {
        var ids = new List<EntityId>();
        var enittyIds = (from val in creator.entityIdToSpatialEntity
                         where val.Value.HasComponent<MoveCube.Component>()
                         select val.Key).ToList();

        var count = Mathf.Min(amount, enittyIds.Count);
        for (int i = 0; i < count; ++i)
        {
            ids.Add(enittyIds[i]);
        }

        for (int i = 0; i < ids.Count; ++i)
        {
            commandSystem.SendCommand(new WorldCommands.DeleteEntity.Request(ids[i]));
        }
    }

    private void Connector_OnCreatorCreated(ServerGameObjectCreator obj)
    {
        creator = obj;
    }

    protected override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isOpen = !isOpen;
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
            GUILayout.Label("Server Statistics:");
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("FPS", GUILayout.Width(135f));
                    GUILayout.TextArea($"cur: {(1.0f / Time.smoothDeltaTime).ToString("F2")}, " +
                        $"avg: {(Time.frameCount / Time.time).ToString("F2")}");
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("EntityAmount", GUILayout.Width(135f));
                    GUILayout.TextArea($"{creator?.GetEntityAmount()}");
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Pos Update Send", GUILayout.Width(135f));
                    ServerMetrics.Instance.GetResult(out var resultSent, out var resultReceived, out var resultRatio);
                    GUILayout.TextArea($"{resultSent}");
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("MaxServerMoveCubes", GUILayout.Width(135f));
                    string strCubes = null;
                    strCubes = GUILayout.TextField(strMaxMoveCubes);
                    if (strMaxMoveCubes != strCubes)
                    {
                        strMaxMoveCubes = strCubes;
                        if (int.TryParse(strMaxMoveCubes, out var nCubes))
                        {
                            nMaxMoveCubes = nCubes;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndArea();
    }
}
