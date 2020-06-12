using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Utility
{
    public static T[] ShuffleArray<T>(T[] array, int seed)
    {
        System.Random prng = new System.Random(seed);

        for (int i = 0; i < array.Length - 1; i++)
        {
            int randomIndex = prng.Next(i, array.Length);

            T tempItem = array[randomIndex];
            array[randomIndex] = array[i];
            array[i] = tempItem;
        }

        return array;
    }

    public static void SetStaticEditorFlag(GameObject obj, StaticEditorFlags flag, bool shouldEnable)
    {
        var currentFlags = GameObjectUtility.GetStaticEditorFlags(obj);

        if (shouldEnable)
        {
            currentFlags |= flag;
        } else
        {
            currentFlags &= ~flag;
        }

        GameObjectUtility.SetStaticEditorFlags(obj, currentFlags);
    }
}
