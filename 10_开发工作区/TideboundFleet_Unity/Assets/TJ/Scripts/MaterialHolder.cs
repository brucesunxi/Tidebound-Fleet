using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="MaterialHolder",fileName ="materialsHolder")]
public class MaterialHolder : ScriptableObject
{
    public List<Material> materialsList;

    private Dictionary<ColorEnum, Material> materialDictionary;

    public void InitializeMaterialDictionary()
    {
        materialDictionary = new Dictionary<ColorEnum, Material>();

        foreach (Material m in materialsList)
        {
            foreach (ColorEnum color in System.Enum.GetValues(typeof(ColorEnum)))
            {
                if (m.name.Contains(color.ToString()))
                {
                    materialDictionary[color] = m;
                    break;
                }
            }
        }
    }

    public Material FindMaterialByName(ColorEnum type)
    {
        //if (materialDictionary == null)
        //{
        //    InitializeMaterialDictionary();
        //}

        if (materialDictionary.ContainsKey(type))
        {
            return materialDictionary[type];
        }
        else
        {
            Debug.Log("Material Not Found With the color " + type);
            return null;
        }
    }

    public List<Material> GetRandomMaterials(int count)
    {
        if (materialsList == null || materialsList.Count == 0)
        {
            Debug.Log("Material list is empty!");
            return null;
        }

        // Shuffle the materialsList
        List<Material> shuffledMaterials = new List<Material>(materialsList);
        System.Random rng = new System.Random();
        int n = shuffledMaterials.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Material value = shuffledMaterials[k];
            shuffledMaterials[k] = shuffledMaterials[n];
            shuffledMaterials[n] = value;
        }

        // Take the first 'count' materials from the shuffled list
        List<Material> randomMaterials = new List<Material>();
        for (int i = 0; i < Mathf.Min(count, shuffledMaterials.Count); i++)
        {
            randomMaterials.Add(shuffledMaterials[i]);
        }

        return randomMaterials;
    }
}
