using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using HandyUtilities;

namespace SpriteToMeshConverter
{
    [InitializeOnLoad]
    sealed class SpriteConverter : EditorWindow
    {
        internal static SpriteConverterData data;

        static string assetFilePath
        {
            get { return !data.combine ? string.Format(@"{0}/{1}", data.directoryPath, data.assetName) : string.Format(@"{0}", data.directoryPath); }
        }

        [MenuItem("Tools/Sprite Converter")]
        public static void ShowWindow()
        {
            EditorWindow win = GetWindow<SpriteConverter>("Sprite Converter", true);
            win.maxSize = new Vector2(310, 470);
            win.minSize = new Vector2(310, 470);
            if (data == null)
            {
                data = AssetDatabase.LoadAssetAtPath<SpriteConverterData>(SpriteConverterData.DATA_PATH);
                if (data == null)
                {
                    data = SpriteConverterData.Create();
                }
            }

            if (data.shader == null)
            {
                data.shader = Shader.Find("Sprites/Default");
                EditorUtility.SetDirty(data);  
            }
            if(data.sprite)
            {
                data.size = new Vector2(data.sprite.rect.width / data.pixelsPerUnit, data.sprite.rect.height / data.pixelsPerUnit);
                EditorUtility.SetDirty(data);  
            }
        }

        void OnGUI()
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 110, 20), "Select Sprite:");
            var s = data.sprite; 
            data.sprite = EditorGUI.ObjectField(new Rect(rect.x, rect.y, 100, 100), data.sprite, typeof(Sprite), true) as Sprite;
            if (data.sprite == null) return;
            GUILayout.Space(5);
            var title = new GUIStyle() { alignment = TextAnchor.MiddleCenter };
            data.textureHeight = Mathf.Clamp(data.textureHeight, 1, (int) data.sprite.rect.height);
            data.textureWidth = Mathf.Clamp(data.textureWidth, 1, (int) data.sprite.rect.width);
            rect.width = Mathf.Clamp(rect.width, 270, 300);
            rect.x = 5;
            rect.y += 20;
            if (data.sprite != s)
            {
                var assetPath = AssetDatabase.GetAssetPath(data.sprite);
                data.directoryPath = Path.GetDirectoryName(assetPath);
                data.assetName = data.sprite.name;
                data.directoryPath = Path.GetDirectoryName(assetPath);
                data.assetName = data.sprite.name;
                data.textureHeight = (int) data.sprite.rect.height;
                data.textureWidth = (int) data.sprite.rect.width;
                data.size = new Vector2(data.sprite.rect.width / data.pixelsPerUnit, data.sprite.rect.height / data.pixelsPerUnit);
                data.texSize = 1;
            }
            rect.y += 110;
            if (data.sprite != null)
            {
                title.fontStyle = FontStyle.Bold;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 20), "Options", title);
                rect.y += 30;
                var ppu = data.pixelsPerUnit;
                data.pixelsPerUnit = EditorGUI.Slider(new Rect(rect.x, rect.y, rect.width, 20), "Pixels Per Unit", data.pixelsPerUnit, 1, 1024);
                if (data.pixelsPerUnit != ppu)
                {
                    data.size = new Vector2(data.sprite.rect.width / ppu, data.sprite.rect.height / ppu);
                }
                rect.y += 20;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, 20), "Size:");
                rect.y += 20;
                var sizeX = data.size.x;
                var sizeY = data.size.y;

                data.size = EditorGUI.Vector2Field(new Rect(rect.x, rect.y, rect.width, 20), "", data.size);

                if(data.size.x != sizeX)
                {
                    data.size.x = Mathf.Clamp(data.size.x, 0.001f, float.MaxValue);
                    data.pixelsPerUnit = data.sprite.rect.width / data.size.x;
                    data.size.y = data.sprite.rect.height / data.pixelsPerUnit;
                    data.size.y = Mathf.Clamp(data.size.y, 0.001f, float.MaxValue);
                }
                else if (data.size.y != sizeY)
                {
                    data.size.y = Mathf.Clamp(data.size.y, 0.001f, float.MaxValue);
                    data.pixelsPerUnit = data.sprite.rect.height / data.size.y;
                    data.size.x = data.sprite.rect.width / data.pixelsPerUnit;
                    data.size.x = Mathf.Clamp(data.size.x, 0.001f, float.MaxValue);
                }
                rect.y += 20;
                data.useSkinnedMesh = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width, 20), " Use Skinned Mesh", data.useSkinnedMesh);
                rect.y += 20;
                data.useDecal = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width, 20), " Use Decal", data.useDecal);
                rect.y += 20;
                if(GUI.Button(new Rect(rect.x + 50, rect.y, rect.width - 50, 20), data.shader.name, EditorStyles.popup))
                {
                    HandyEditor.DisplayShaderContext(rect, data.shader, new MenuCommand(this));
                }
                GUI.Label(new Rect(rect.x, rect.y, rect.width, 20), "Shader");
                rect.y += 20;
                if (data.useDecal)
                {
                    data.decalSize = EditorGUI.Slider(new Rect(rect.x, rect.y, rect.width, 15), string.Format("Decal Size: {0} %", Mathf.FloorToInt((data.decalSize * 100))), data.decalSize, 0.01f, 1f);
                    rect.y += 20;
                }
                data.useSpriteMesh = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width, 20), " Use Sprite Mesh", data.useSpriteMesh);
                rect.y += 20;
                data.combine = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width, 20), " Combine Into One Asset File", data.combine);
                rect.y += 20;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, 90, 20), "Asset Name: ");
                data.assetName = EditorGUI.TextField(new Rect(rect.x + 90, rect.y, rect.width - 90, 20), data.assetName);
                rect.y += 30;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, 90, 20), "Asset Path: ");
                EditorGUI.LabelField(new Rect(rect.x + 90, rect.y, rect.width - 90, 20), data.directoryPath);
                rect.y += 30;
                if (GUI.Button(new Rect(rect.width - 60, rect.y, 60, 20), "Change"))
                {
                    var p = EditorUtility.OpenFolderPanel("Select folder", data.directoryPath, "");
                    data.directoryPath = p != string.Empty ? Helper.ConvertLoRelativePath(p) : data.directoryPath;
                }
                rect.y += 30;

                if (GUI.Button(new Rect(rect.x, rect.y, rect.width, 20), "Generate"))
                {
                    Generate();
                }
                rect.y += 20;
            }
            else
            {
                data.assetName = null;
                data.directoryPath = null;
            }
            rect.y += 20;

            EditorUtility.SetDirty(data);
        }

        void Generate()
        {
            var mesh = data.useSpriteMesh ? GenerateMeshFromSprite(data.sprite, data.pixelsPerUnit) : GenerateQuadMeshFromSprite(data.sprite, data.pixelsPerUnit);
            if (data.combine)
            {
             
                SavePrefabFromSprite(mesh, data.assetName, data.directoryPath);
            }
            else
            {
                SavePrefabFolderFromSprite(mesh, data.assetName, data.directoryPath);
            }
        }
      
        /// <summary>
        /// Creates quad mesh from sprite
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="ppu"></param>
        /// <returns></returns>
        public static Mesh GenerateQuadMeshFromSprite(Sprite sprite, float ppu)
        {
            var pivot = sprite.pivot;
            var rect = sprite.textureRect;
            var worldHeight = rect.height / ppu;
            var worldWidth = rect.width / ppu;
            pivot.x /= rect.width;
            pivot.y /= rect.height;
            pivot.x *= worldWidth;
            pivot.y *= worldHeight;

            Vector3[] vertices = new Vector3[] 
            {
                new Vector3(-pivot.x, -pivot.y),
                new Vector3(-pivot.x, worldHeight - pivot.y),
                new Vector3(worldWidth - pivot.x, worldHeight - pivot.y),
                new Vector3(worldWidth - pivot.x, -pivot.y)
            };

            Vector2[] uv = new Vector2[4];

            var textureWidth = sprite.texture.width;
            var textureHeight = sprite.texture.height;
            var spritePosX = sprite.rect.x;
            var spritePosY = sprite.rect.y;

            uv[0] = new Vector2(spritePosX / textureWidth, spritePosY / textureHeight);
            uv[1] = new Vector2(spritePosX / textureWidth, (spritePosY + rect.height) / textureHeight);
            uv[2] = new Vector2((spritePosX + sprite.rect.width) / textureWidth, (spritePosY + rect.height) / textureHeight);
            uv[3] = new Vector2((spritePosX + sprite.rect.width) / textureWidth, spritePosY / textureHeight);

            Mesh m = new Mesh();
            m.vertices = vertices;
            m.triangles = new int[] { 0, 1, 2, 2, 3, 0 };
            m.uv = uv;
            m.uv2 = new Vector2[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
            m.RecalculateBounds();
            m.RecalculateNormals();
            return m;
        }

        /// <summary>
        /// Generates mesh from sprite using its vertives, uvs and triangles
        /// </summary>
        /// <param name="sprite">Source sprite</param>
        /// <param name="ppu">Pixels per unit</param>
        /// <returns></returns>
        public static Mesh GenerateMeshFromSprite(Sprite sprite, float ppu)
        {
            var vertices = sprite.vertices.ToVector3Array();
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                v *= sprite.pixelsPerUnit / ppu;
                vertices[i] = v;
            }
            int[] triangles = new int[sprite.triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = sprite.triangles[i];
            }
            Mesh m = new Mesh();
            m.vertices = vertices;
            m.triangles = triangles;
            m.uv = sprite.uv;
            var uv2 = new Vector2[m.uv.Length];
            var uv = sprite.uv;
            for (int i = 0; i < uv2.Length; i++)
            {
                var uvPoint = uv[i];
                var txtPixelPoint = new Vector2(uvPoint.x * sprite.texture.width, uvPoint.y * sprite.texture.height);
                var spriteUV = new Vector2();
                spriteUV.x = Helper.Remap(txtPixelPoint.x, sprite.rect.x, sprite.rect.x + sprite.rect.width, 0f, 1f);
                spriteUV.y = Helper.Remap(txtPixelPoint.y, sprite.rect.y, sprite.rect.y + sprite.rect.height, 0f, 1f);
                uv2[i] = spriteUV;
            }
            m.uv2 = uv2;
            m.RecalculateBounds();
            m.RecalculateNormals();
            return m;
        }

        /// <summary>
        /// Returns sprite scale which can be applied to material scale propery in order to match whole texture
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public static Vector2 GetSpriteScale(Sprite sprite)
        {
            var dw = sprite.texture.width / sprite.rect.width;
            var dh = sprite.texture.height / sprite.rect.height;
            return new Vector2(dw, dh);
        }

        /// <summary>
        /// Returns sprite offset which can be applied to material offset propery in order to match whole texture
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public static Vector2 GetSpriteOffset(Sprite sprite)
        {
            return new Vector2(-sprite.rect.x / (sprite.rect.width), -sprite.rect.y / (sprite.rect.height));
        }

        /// <summary>
        /// Saves texture from sprite to disk at specified relative asset path
        /// </summary>
        /// <param name="sprite"> Sprite to tale texture from</param>
        /// <param name="relativePath"> Relative path to save texture to</param>
        /// <param name="clear"> If checked, creates clear texture in stead of capturing from sprite source texture</param>
        /// <returns></returns>
        public static Texture2D SaveTextureFromSprite(Sprite sprite,bool clear)
        {
            var t = new Texture2D((int) sprite.rect.width, (int) sprite.rect.height, TextureFormat.ARGB32, true);
            var offset = new Vector2(sprite.rect.x, sprite.rect.y);
            bool txtIsReadable;
            MakeTextureReadable(sprite.texture, true, out txtIsReadable);
            for (int x = 0; x < t.width; x++)
            {
                for (int y = 0; y < t.height; y++)
                {
                    var c = sprite.texture.GetPixel((int) (x + offset.x), (int) (y + offset.y));
                    if (clear)
                        c.a = 0;
                    t.SetPixel(x, y, c);
                    
                }
            }
            t.Apply();
            return t;
        }

        /// <summary>
        /// Makes texture readable or not
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="readable"></param>
        public static void MakeTextureReadable(Texture2D txt, bool readable)
        {
            var path = AssetDatabase.GetAssetPath(txt);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (readable != imp.isReadable)
            {
                imp.isReadable = readable;
                imp.SaveAndReimport();
            }
        }

        /// <summary>
        /// Makes texture readable or not and gives out whether or not it was readable at the moment.
        /// </summary>
        /// <param name="txt">Source texture</param>
        /// <param name="readable">Readable or not</param>
        /// <param name="isReadableBefore">Returns out value before proceeding</param>
        public static void MakeTextureReadable(Texture2D txt, bool readable, out bool isReadableBefore)
        {
            var path = AssetDatabase.GetAssetPath(txt);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            isReadableBefore = imp.isReadable;
            if(readable != imp.isReadable)
            {
                imp.isReadable = readable;
                imp.SaveAndReimport();
            }
        }

        /// <summary>
        /// Checks if texture is readable
        /// </summary>
        /// <param name="txt">Source texture</param>
        /// <returns></returns>
        public static bool IsTextureReadable(Texture2D txt)
        {
            var path = AssetDatabase.GetAssetPath(txt);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            return imp.isReadable;
        }

        private void OnSelectedShaderPopup(string command, Shader shader)
        {
            if (shader != null)
            {
                data.shader = shader;
                EditorUtility.SetDirty(data);  
            }
        }
        /// <summary>
        /// Creates prefab from sprite
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="name"></param>
        /// <param name="path"></param>
        public static void SavePrefabFromSprite(Mesh mesh, string name, string path)
        {
            var obj = new GameObject(name);
            mesh.name = "mesh";
            var prefabPath = path + "/" + name + ".prefab";
            var mf = obj.AddComponent<MeshFilter>();
            var mr = obj.AddComponent<MeshRenderer>();

            var mat = new Material(data.shader);
            var prefab = PrefabUtility.CreatePrefab(prefabPath, obj);
            mat.name = "material";
            mat.mainTexture = data.sprite.texture;
            AssetDatabase.AddObjectToAsset(mesh, prefab);
            AssetDatabase.AddObjectToAsset(mat, prefab);
            if(data.useDecal)
            {
                var decal = SaveTextureFromSprite(data.sprite, true);
                if(data.decalSize != 1)
                {
                    TextureResizing.Bilinear(decal, (int)(decal.width * data.decalSize), (int) (decal.height * data.decalSize));
                }
                decal.wrapMode = TextureWrapMode.Clamp;
                decal.name = "decal";
                AssetDatabase.AddObjectToAsset(decal, prefab);
                if(mat.HasProperty("_DecalTex"))
                {
                    mat.SetTexture("_DecalTex", decal);
                }
            }
            mf = prefab.GetComponent<MeshFilter>();
            mr = prefab.GetComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = mat;
            DestroyImmediate(obj);
            AssetDatabase.ImportAsset(prefabPath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        public static void SavePrefabFolderFromSprite(Mesh mesh, string name, string path)
        {
            var obj = new GameObject(name);
            mesh.name = "mesh";
            path += "/" + name;
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var prefabPath = path + "/" + name + ".prefab";
            var matPath = path + "/material.mat";
            var meshPath = path + "/mesh.asset";


            var mf = obj.AddComponent<MeshFilter>();
            var mr = obj.AddComponent<MeshRenderer>();

            var mat = new Material(data.shader);
            var prefab = PrefabUtility.CreatePrefab(prefabPath, obj);
            mat.name = "material";
            mat.mainTexture = data.sprite.texture;
            AssetDatabase.CreateAsset(mesh, meshPath);
            AssetDatabase.CreateAsset(mat, matPath);
            if (data.useDecal)
            {
                var decalPath = path + "/decal.png";
                var decal = SaveTextureFromSprite(data.sprite, true);
                if (data.decalSize != 1)
                {
                    TextureResizing.Bilinear(decal, (int) (decal.width * data.decalSize), (int) (decal.height * data.decalSize));
                }
                decal.wrapMode = TextureWrapMode.Clamp;
                decal.name = "decal";
                var b = decal.EncodeToPNG();
                File.WriteAllBytes(Application.dataPath.Remove(Application.dataPath.Length - 6, 6) + decalPath, b);
                AssetDatabase.ImportAsset(decalPath);
                TextureImporter output = (TextureImporter) AssetImporter.GetAtPath(decalPath);
                output.alphaIsTransparency = true;
                output.textureType = TextureImporterType.Advanced;
                output.isReadable = true;
                output.textureFormat = TextureImporterFormat.RGBA32;
                output.wrapMode = TextureWrapMode.Repeat;
                output.normalmapFilter = TextureImporterNormalFilter.Standard;
                output.npotScale = TextureImporterNPOTScale.ToNearest;
                output.spriteImportMode = SpriteImportMode.None;
                output.SaveAndReimport();
                
                if (mat.HasProperty("_DecalTex"))
                {
                    mat.SetTexture("_DecalTex", AssetDatabase.LoadAssetAtPath<Texture2D>(decalPath));
                }
            }
            mf = prefab.GetComponent<MeshFilter>();
            mr = prefab.GetComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = mat;
            DestroyImmediate(obj);
            AssetDatabase.ImportAsset(prefabPath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        public static Texture2D CreateBlankTexture(Rect source, float scale = 1f)
        {
            var d = new Texture2D((int) (source.width * scale), (int) (source.height * scale), TextureFormat.ARGB32, true);
            var p = new Color[d.width * d.height];
            for (int i = 0; i < p.Length; i++)
            {
                p[i] = new Color(0, 0, 0, 0);
            }
            d.SetPixels(p);
            d.Apply();
            return d;
        }

        public static Texture2D CaptureTextureFromSprite(Sprite sprite, string path)
        {
            TextureImporter original = (TextureImporter) AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite.texture));
            var t_format = original.textureFormat;
            var t_type = original.textureType;
            var t_isReadable = original.isReadable;
            original.textureType = TextureImporterType.Advanced;
            original.isReadable = true;
            original.textureFormat = TextureImporterFormat.ARGB32;
            original.SaveAndReimport();
            var relativeRect = sprite.rect;
            var map = new Texture2D((int) (relativeRect.width), (int) (relativeRect.height), TextureFormat.ARGB32, false, true);
            var pixels = new List<Color>(map.height * map.width);
            var xStart = (int) relativeRect.x;
            var yStart = (int) relativeRect.y;
            for (int y = yStart; y < yStart + map.height; y++)
            {
                for (int x = xStart; x < xStart + map.width; x++)
                {
                    pixels.Add(sprite.texture.GetPixel(x, y));
                }
            }
            map.SetPixels(pixels.ToArray());
            map.Apply();
            original.textureType = t_type;
            original.isReadable = t_isReadable;
            original.textureFormat = t_format;
            original.SaveAndReimport();
            if (map.width != data.textureWidth || map.height != data.textureHeight)
                TextureResizing.Bilinear(map, data.textureWidth, data.textureHeight);
            var b = map.EncodeToPNG();
            File.WriteAllBytes(path, b);
            AssetDatabase.ImportAsset(path);
            TextureImporter output = (TextureImporter) AssetImporter.GetAtPath(path);
            output.alphaIsTransparency = true;
            output.textureType = TextureImporterType.Advanced;
            output.isReadable = true;
            output.textureFormat = TextureImporterFormat.RGBA32;
            output.wrapMode = TextureWrapMode.Repeat;
            output.normalmapFilter = TextureImporterNormalFilter.Standard;
            output.npotScale = TextureImporterNPOTScale.ToNearest;
            output.spriteImportMode = SpriteImportMode.None;
            output.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

    }

}
