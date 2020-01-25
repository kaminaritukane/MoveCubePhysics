using BlankProject;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class EventObserverCommandSender : MonoBehaviour
{
    [Require] private SvTestCommandCommandSender commandSender = null;
    [Require] private EntityId entityId = default;

    private delegate void UpdateMoveCubeParams(ref int count);

    //private bool canSendCmd = true;
    //private float lastSentTime = 0.0f;

    private Rect areaRect = new Rect(350, 130, 300f, 200f);
    private bool isOpen = false;

    //private uint cmdCount = 0;
    //private float commandRespondTime = 10.0f; // 10 mean infinite

    private ClientGameObjectCreator creator;

    private string strMaxMoveCubes;
    private int nMaxMoveCubes = -1;

    private CancellationTokenSource tokenSource;

    private bool isSpawningOrRemoving = false;

    private async void Start()
    {
        strMaxMoveCubes = nMaxMoveCubes.ToString();

        var clientWorker = GameObject.FindObjectOfType<UnityClientConnector>();
        if (clientWorker != null)
        {
            var connector = clientWorker.GetComponent<UnityClientConnector>();
            creator = connector.TransformGameObjectCreator;
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
        catch(TaskCanceledException)
        {

        }
    }

    private async Task Monitor(int maxCubes,
        UpdateMoveCubeParams updateParams)
    {
        var token = tokenSource.Token;

        while( true )
        {
            if ( token.IsCancellationRequested )
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

    private void Update()
    {
        //if (canSendCmd)
        //{
        //    // Get Ping Time
        //    commandSender.SendTestCommandCommand(
        //        entityId,
        //        new BlankProject.Empty(),
        //        (resp) =>
        //        {
        //            canSendCmd = true;

        //            commandRespondTime = Time.time - lastSentTime;
        //        });

        //    canSendCmd = false;
        //    lastSentTime = Time.time;
        //    cmdCount++;
        //}

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isOpen = !isOpen;
            if (isOpen)
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

    private void SpawnMoveCubes(int count, CancellationToken token)
    {
        commandSender?.SendSpawnMovecubeCommand(entityId,
            new SpawnRequest
            {
                PrefixName = $"MoveCube_",
                Amount = count
            },
            (resp) =>
            {
                Debug.Log($"MoveCube create responsed. Msg:{resp.Message}");
                if (resp.StatusCode == Improbable.Worker.CInterop.StatusCode.Success)
                {
                }
            }
        );
    }

    private void DeleteMoveCubes(int amount)
    {
        var ids = new List<EntityId>();
        var enittyIds = (from val in creator.entityIdToSpatialEntity
                   where val.Value.HasComponent<MoveCube.Component>()
                   select val.Key).ToList();

        var count = Mathf.Min(amount, enittyIds.Count);
        for ( int i=0; i<count; ++i )
        {
            ids.Add(enittyIds[i]);
        }
        commandSender.SendDeleteMovecubeCommand(entityId,
            new MoveCubeInfo(ids),
            (resp) =>
            {
                Debug.Log($"MoveCubes delete responsed. Msg: {resp.Message}");
            }
        );
    }

    private void OnGUI()
    {
        if (!isOpen)
        {
            return;
        }

        GUILayout.BeginArea(areaRect, GUI.skin.box);
        {
            GUILayout.Label("Client Statistics:");
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("FPS", GUILayout.Width(135f));
                    GUILayout.TextArea($"cur: {(1.0f / Time.smoothDeltaTime).ToString("F2")}, " +
                        $"avg: {(Time.frameCount / Time.time).ToString("F2")}");
                }
                GUILayout.EndHorizontal();
                //GUILayout.BeginHorizontal();
                //{
                //    GUILayout.Label("CmdResp", GUILayout.Width(135f));
                //    GUILayout.TextArea($"CmdNum: {cmdCount}\n" +
                //        $"Time: {commandRespondTime.ToString("F2")}\n" +
                //        $"Freq: {(1.0f / commandRespondTime).ToString("F2")}");
                //}
                //GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("EntityAmount", GUILayout.Width(135f));
                    GUILayout.TextArea($"{creator?.GetEntityAmount()}");
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