using UnityEditor;

[CustomEditor(typeof(CityAgent))]
public class DynamicRange : Editor {

    public override void OnInspectorGUI(){
        
        base.OnInspectorGUI();

        CityAgent script = (CityAgent)target;

        script.numberOfHouse = EditorGUILayout.IntSlider("Number Of House", script.numberOfHouse, 1, script.MaxNHouse);
    }

}
