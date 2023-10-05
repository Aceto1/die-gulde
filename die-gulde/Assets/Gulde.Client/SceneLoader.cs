using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gulde.Client.Model.Scenes;
using Newtonsoft.Json;
using Siccity.GLTFUtility;
using UnityEngine;
using static Siccity.GLTFUtility.GLTFAccessor.Sparse;

namespace Gulde.Client
{
    public class SceneLoader : MonoBehaviour
    {
        public static void LoadScene(string basePath, string scenePath)
        {
            var objectNames = new Dictionary<string, string>();
        
            var objectsPath = Path.Combine(basePath, "objects");
            var files = Directory.GetFiles(objectsPath, "*.glb", SearchOption.AllDirectories);

            foreach (var item in files)
            {
                var key = Path.GetFileNameWithoutExtension(item);

                if (objectNames.ContainsKey(key))
                    continue;

                objectNames.Add(key, item);
            }

            scenePath = Path.Combine(basePath, scenePath);
            var sceneAsString = File.ReadAllText(scenePath);

            var gildeScene = JsonConvert.DeserializeObject<GildeScene>(sceneAsString);

            LoadSceneElements(gildeScene, objectNames);
        }

        static void LoadSceneElements(GildeScene gildeScene, Dictionary<string, string> objects)
        {
            var mainParentObject = new GameObject("scene_elements");
            var currentGroupParent = mainParentObject.transform;
            var currentParent = mainParentObject.transform;

            var sceneElementToGameObject = new Dictionary<SceneElement, GameObject>();

            for (var i = 0; i < gildeScene.SceneElements.Length; i++)
            {
                var element = gildeScene.SceneElements[i];

                if (element.OnesCount == 1)
                    currentParent = mainParentObject.transform;

                if (!currentParent)
                {
                    Debug.LogWarning($"The parent of element \"{element.Name}\" was not found.");
                    currentParent = mainParentObject.transform;
                }

                if (element.TransformElement is not null)
                {
                    var gameObject = LoadTransformElement(element, currentParent, objects);
                    sceneElementToGameObject.Add(element, gameObject);
                }
                else if (element.CityElement is not null)
                {
                    var gameObject = LoadCityElement(element.Width, element.Height, element.CityElement, currentParent);
                    sceneElementToGameObject.Add(element, gameObject);
                }
                else
                {
                    var gameObject = new GameObject(element.Name);
                    ApplyTransform(gameObject, null, currentParent);
                    sceneElementToGameObject.Add(element, gameObject);
                }

                if (!sceneElementToGameObject[element])
                    continue;

                if (element.OnesCount == 1)
                    currentGroupParent = sceneElementToGameObject[element].transform;

                if (element.Hierarchy == 0)
                    currentParent = sceneElementToGameObject[element].transform;
                else if (element.Hierarchy == 2)
                    currentParent = currentGroupParent;
            }

            Debug.Log("Loaded scene elements.");
        }

        static GameObject LoadTransformElement(SceneElement sceneElement, Transform parent, Dictionary<string, string> objects)
        {
            var gameObject = (GameObject)null;
            
            if (sceneElement.TransformElement is DummyElement dummyElement)
            {
                gameObject = new GameObject(sceneElement.Name);
            }
            else if (sceneElement.TransformElement is ObjectElement objectElement)
            {
                if (objectElement.Name is not null && objects.TryGetValue(objectElement.Name, out var gltfPath))
                {
                    gameObject = LoadObject(gltfPath);

                    if (gameObject is null)
                    {
                        return null;
                    }
                    
                    gameObject.name = objectElement.Name;
                }
                else
                {
                    Debug.LogWarning($"Object \"{objectElement.Name}\" not found.");
                }
            }
            
            if (gameObject is null)
                return null;
            
            ApplyTransform(gameObject, sceneElement, parent);
            
            return gameObject;
        }

        static void ApplyTransform(GameObject gameObject, SceneElement sceneElement = null, Transform parentObject = null)
        {
            if (parentObject is not null)
            {
                gameObject.transform.SetParent(parentObject);
            }
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;

            var transformElement = sceneElement?.TransformElement;
            if (transformElement is null)
                return;
            
            var position = new Vector3(-transformElement.Transform.Position.x, transformElement.Transform.Position.y, -transformElement.Transform.Position.z);
            var rotation = new Vector3(transformElement.Transform.Rotation.x, -transformElement.Transform.Rotation.y, transformElement.Transform.Rotation.z); //  * (parentObject is null ? -1 : 1)
            // var rotation = new Vector3(0f, -transformElement.Transform.Rotation.y, 0f);

            // if (gildeGroup is not null)
            // {
            //     var groupElement = gildeGroup.Elements.FirstOrDefault(element => element.Name == sceneElement.Name);
            //
            //     if (groupElement is not null)
            //     {
            //         var groupPosition = new Vector3(-groupElement.Transform.Position.x, groupElement.Transform.Position.y, -groupElement.Transform.Position.z);
            //         var groupRotation = new Vector3(groupElement.Transform.Rotation.x, -groupElement.Transform.Rotation.y,
            //             groupElement.Transform.Rotation.z);
            //         
            //         position += groupPosition;
            //         rotation += groupRotation;
            //     }
            // }

            //// Initialize an identity matrix
            //Matrix4x4 matrix = Matrix4x4.identity;

            //// Apply translation
            //matrix.m03 = position.x;
            //matrix.m13 = position.y;
            //matrix.m23 = position.z;

            //// Apply scaling
            //matrix.m00 = rotation.x;
            //matrix.m11 = 1.0f;  // Set to 1.0 for pure scaling
            //matrix.m22 = rotation.z;

            //// Apply rotation
            //float cosTheta = Mathf.Cos(rotation.y);
            //float sinTheta = Mathf.Sin(rotation.y);
            //matrix.m00 = cosTheta;
            //matrix.m02 = sinTheta;
            //matrix.m20 = -sinTheta;
            //matrix.m22 = cosTheta;

            //// Apply shear
            //var scale = Vector3.one;
            //matrix.m01 = scale.x;
            //matrix.m10 = scale.y;

            //if (gameObject.name == "ub_KISTE_A1")
            //{
            //    Debug.Log(matrix);
            //    //Debug.Log(matrix.GetColumn(3));
            //}

            //// Apply matrix
            // gameObject.transform.localPosition = matrix.GetColumn(3);
            // gameObject.transform.localRotation = matrix.rotation;

            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = Quaternion.Euler(Mathf.Rad2Deg * rotation);
            gameObject.transform.localScale = Vector3.one;
        }
        
        static GameObject LoadCityElement(int width, int height, CityElement cityElement, Transform parentObject)
        {
            var terrainObject = new GameObject("Terrain");
            terrainObject.transform.SetParent(parentObject.transform);
            var terrain = terrainObject.AddComponent<Terrain>();
            var terrainData = new TerrainData();
            
            var heights = new float[width, height];
            var maxHeight = cityElement.HeightData1.Max();
            
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    heights[x, y] = (float)cityElement.HeightData1[x + y * width] / maxHeight;
                }
            }
            
            terrainData.heightmapResolution = width;
            terrainData.size = new Vector3(width, 100, height);
            terrainData.SetHeights(0, 0, heights);
            terrain.terrainData = terrainData;
            
            return terrainObject;
        }

        //static GameObject LoadParentElement(SceneElementGroup elementGroup, Dictionary<string, string> objects)
        //{
        //    var parent = new GameObject(elementGroup.FirstElement.Name);
        //    var sceneElement = elementGroup.FirstElement;
        //    var transformElement = sceneElement.TransformElement;

        //    if (sceneElement is null)
        //    {
        //        Debug.LogWarning("Element group has no first element.");
        //        return null;
        //    }
            
        //    if (transformElement is not null)
        //    {
        //        ApplyTransform(parent, sceneElement);
                
        //        if (transformElement is not ObjectElement objectElement ||
        //            objectElement.Name == null ||
        //            !objects.TryGetValue(objectElement.Name, out var gltfPath)) return parent;
                
        //        var childObject = Importer.LoadFromFile(gltfPath, Format.GLB);
        //        var name = transformElement is ObjectElement element
        //            ? element.Name
        //            : sceneElement.Name;
        //        childObject.name = name;
        //        ApplyTransform(childObject, null, parent);
        //    }
        //    else if (sceneElement.CityElement is not null)
        //    {
        //        LoadCityElement(elementGroup.FirstElement.Width, elementGroup.FirstElement.Height, elementGroup.FirstElement.CityElement, parent);
        //    }

        //    return parent;
        //}

        static GameObject LoadObject(string path)
        {
            if (path.EndsWith("ub_BLUMEN.glb"))
                return new GameObject("Blumen");

            if (path.EndsWith("ub_KRAUT.glb"))
                return new GameObject("Kraut");

            try
            {
                return Importer.LoadFromFile(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not load object {path}\n{e}");
                return null;
            }
        }
    }
}
