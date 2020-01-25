using UnityEngine;


public class SimulatedUnitMetrics : TransformUpdateMetrics
{
    private static SimulatedUnitMetrics instance;
    public static SimulatedUnitMetrics Instance
    {
        get
        {
            return instance;
        }
    }

    private Rect areaRect = new Rect(660, 0, 300f, 130f);
    private bool isOpen = false;

    private void Awake()
    {
        instance = this;
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
            GUILayout.Label("Simu Client Statistics:");
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
                //    GUILayout.Label("Update Ratio", GUILayout.Width(135f));
                //    GetResult(out var resultSent, out var resultReceived, out var resultRatio);
                //    GUILayout.TextArea($"Received/Sent: {resultReceived}/{resultSent}, Ratio: {resultRatio.ToString("p")}");
                //}
                //GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("EntityAmount", GUILayout.Width(135f));
                    GUILayout.TextArea($"{SimulatedUnitManager.Instance.GetEntityAmount()}");
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndArea();
    }
}
