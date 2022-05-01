using UnityEditor;

[CustomEditor(typeof(CityAgent))]
public class DynamicRange : Editor {

    public override void OnInspectorGUI(){
        
        base.OnInspectorGUI();

        CityAgent script = (CityAgent)target;

        script.NumberOfHouse = EditorGUILayout.IntSlider("Number Of House", script.NumberOfHouse, 1, script.maxNHouse);
    }

}
