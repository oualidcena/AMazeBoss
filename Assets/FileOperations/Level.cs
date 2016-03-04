using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.FileOperations
{
    [Serializable]
    public class LevelsInfo
    {
        public List<string> Levels = new List<string>();

        public void AddOrUpdate(string levelName)
        {
            if (!Levels.Contains(levelName))
            {
                Levels.Add(levelName);
            }
        }

        public void Remove(string levelName)
        {
            Levels.Remove(levelName);
        }
    }

    [Serializable]
    public class Level
    {
        public List<LevelObject> Tiles;

        public Level(List<LevelObject> tiles)
        {
            Tiles = tiles;
        }
    }

    [Serializable]
    public class LevelObject
    {
        public string Class;
        public string MainType;
        public string Subtype;
        public int X;
        public int Z;
        public int Rotation;
        public string Descriptors;

        public LevelObject(string mainType, string subtype, int x, int z, int rotation, IEnumerable<string> descriptors)
        {
            Rotation = rotation;
            MainType = mainType;
            Subtype = subtype;
            X = x;
            Z = z;
            Descriptors = string.Join(";", descriptors.ToArray());
        }
    }
}