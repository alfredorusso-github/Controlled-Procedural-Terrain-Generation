using UnityEditor;

[CustomEditor(typeof(CityAgentFlat))]
public class DynamicRange : Editor {

    public override void OnInspectorGUI(){
        
        base.OnInspectorGUI();

        CityAgentFlat script = (CityAgentFlat)target;

        script.NumberOfHouse = EditorGUILayout.IntSlider("Number Of House", script.NumberOfHouse, 1, script.maxNHouse);
    }

}
