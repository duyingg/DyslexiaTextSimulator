using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Preview : MonoBehaviour
{
    public TMP_Text text_Test;
    

    private void Text_Move_Test()
    {
        text_Test.ForceMeshUpdate();
        TMP_TextInfo text_Info_Test = text_Test.textInfo;
        TMP_CharacterInfo charInfo = text_Info_Test.characterInfo[1];
        if (!charInfo.isVisible) Debug.Log("Not visible");
        int vertexIndex = charInfo.vertexIndex;
        int materialIndex = charInfo.materialReferenceIndex;
        Vector3[] vertices = text_Info_Test.meshInfo[materialIndex].vertices;
        Vector3 v0 = vertices[vertexIndex + 0];
        Vector3 v1 = vertices[vertexIndex + 1];
        Vector3 v2 = vertices[vertexIndex + 2];
        Vector3 v3 = vertices[vertexIndex + 3];
        Vector3 center = (v0 + v2) / 2;
        Color32[] colors = text_Info_Test.meshInfo[materialIndex].colors32;

        Vector3 offset = new (40, 0, 0);
        Quaternion rotation = Quaternion.Euler(0, 0, 45);
        float scale = 1.5f;
        Color32 newColor = new(0, 255, 0, 255);

        for (int i = 0; i < 4; i++)
        {
            int vi = vertexIndex + i;
            Vector3 dir = vertices[vi] - center;
            vertices[vi] = center + rotation * dir * scale + offset;
            colors[vi] = newColor;
        }
        text_Test.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

    }
    void Start()
    {
        Text_Move_Test();
    }

    // Update is called once per frame
    void Update()
    {
        Text_Move_Test();
    }
}
