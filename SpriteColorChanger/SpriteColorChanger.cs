using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteColorChanger : EditorWindow
{
    // The target color, default is white
    private Color targetColor = Color.white;

    [MenuItem("Tools/Sprite Color Changer")]
    public static void ShowWindow()
    {
        GetWindow<SpriteColorChanger>("Sprite Color Changer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Change Sprite Color", EditorStyles.boldLabel);
        GUILayout.Label("Select one or more sprites in the Project view, then click the button below.", EditorStyles.wordWrappedLabel);
        
        // Color picker
        targetColor = EditorGUILayout.ColorField("Target Color", targetColor);

        if (GUILayout.Button("Process Selected Sprite(s)"))
        {
            ProcessSelectedSprites();
        }
    }

    private void ProcessSelectedSprites()
    {
        int processedCount = 0;
        int warningCount = 0;
        int errorCount = 0;
        
        Object[] selectedObjects = Selection.objects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Sprite Color Changer", "No objects selected. Please select one or more sprites.", "OK");
            return;
        }

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            EditorUtility.DisplayProgressBar("Processing Sprites",
                $"Processing sprite {i + 1} of {selectedObjects.Length}...",
                (float)i / selectedObjects.Length);

            Object obj = selectedObjects[i];
            string assetPath = AssetDatabase.GetAssetPath(obj);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (texture == null)
            {
                string warn = $"Skipped: Not a Texture2D - {assetPath}";
                Debug.LogWarning(warn);
                warningCount++;
                continue;
            }

            // Ensure texture is readable
            string texturePath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            // Create output directory
            string folderPath = Path.GetDirectoryName(assetPath);
            string outputDir = Path.Combine(folderPath, "Output");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Create a new texture in a readable/writable format
            Texture2D newTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            Color[] pixels = texture.GetPixels();

            // Process each pixel: set its RGB to the target color and combine the alphas
            for (int j = 0; j < pixels.Length; j++)
            {
                float newAlpha = targetColor.a * pixels[j].a;
                pixels[j] = new Color(targetColor.r, targetColor.g, targetColor.b, newAlpha);
            }

            newTexture.SetPixels(pixels);
            newTexture.Apply();

            // Encode to PNG
            byte[] pngData = newTexture.EncodeToPNG();
            if (pngData != null)
            {
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                string hexColor = ColorUtility.ToHtmlStringRGBA(targetColor);
                string newFilePath = Path.Combine(outputDir, $"{fileName}_{hexColor}.png");
                File.WriteAllBytes(newFilePath, pngData);
                Debug.Log($"Saved sprite to: {newFilePath}");
                processedCount++;
            }
            else
            {
                string err = $"Error: Failed to encode texture to PNG for {assetPath}";
                Debug.LogError(err);
                errorCount++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        // Simple summary in popup dialog
        string summary = $"Processed: {processedCount} of {selectedObjects.Length}\n" +
                         $"Warnings: {warningCount}\n" +
                         $"Errors: {errorCount}\n\n" +
                         "For detailed information, please check the Console.";
        Debug.Log(summary);
        if(processedCount != selectedObjects.Length)
            EditorUtility.DisplayDialog("Sprite Color Changer", summary, "OK");
        else
            EditorUtility.DisplayDialog("Sprite Color Changer", $"Successfully processed {processedCount} images.", "OK");
        
    }
}
