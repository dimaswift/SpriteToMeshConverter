using UnityEngine;
using UnityEditor;
using System.Collections;
namespace SpriteToMeshConverter
{
    [System.Serializable]
    internal sealed class SpriteConverterData : ScriptableObject
    {
        internal const string DATA_PATH = "Assets/SpriteConverterData.asset";
        public string assetName, directoryPath;
        public float texSize = 1, decalSize = 1;
        public bool useSpriteMesh = true, useDecal = false, combine = false, useSkinnedMesh = false;
        public Sprite sprite;
        public Shader shader;
        public Vector2 size = Vector2.one;
        public float pixelsPerUnit = 128;
        public int textureHeight = 1, textureWidth = 1;

        public static SpriteConverterData Create()
        {
            var data = CreateInstance<SpriteConverterData>();
            AssetDatabase.CreateAsset(data, "Assets/SpriteConverterData.asset");
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<SpriteConverterData>("Assets/SpriteConverterData.asset");
        }
    }
    [CustomEditor(typeof(SpriteConverterData))]
    public class SpriteConverterDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {

        }
    }

}
