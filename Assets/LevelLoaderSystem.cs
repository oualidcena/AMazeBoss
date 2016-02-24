﻿using System;
using System.Collections.Generic;
using System.Linq;
using Assets.FileOperations;
using Entitas;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets
{
    public class ReturnToPreviousViewSystem : IExecuteSystem
    {
        public void Execute()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                SceneSetup.LoadPreviousScene();
            }
        }
    }

    public class LevelClearedSystem : IReactiveSystem, ISetPool, IEnsureComponents
    {
        private Pool _pool;

        public TriggerOnEvent trigger { get { return GameMatcher.ActingActions.OnEntityRemoved(); } }
        public IMatcher ensureComponents { get { return Matcher.AllOf(GameMatcher.Boss, GameMatcher.Dead); } }

        public void SetPool(Pool pool)
        {
            _pool = pool;
        }

        public void Execute(List<Entity> entities)
        {
            var boss = entities.SingleEntity();
            if (!boss.IsActing())
            {
                try
                {
                    PlaySetup.LevelPath = GetNext(PlaySetup.LevelPath);
                    SceneSetup.LoadScene("Play");
                }
                catch (Exception)
                {
                    SceneManager.LoadScene("gameover");
                }
            }
        }

        private string GetNext(string path)
        {
            try
            {
                var levels = _pool.levels.Value;
                return levels[levels.IndexOf(path) + 1];
            }
            catch (Exception)
            {
                throw new Exception("Unable to find level after " + path);
            }
        }
    }

    public class PlayerRestartSystem : IExecuteSystem
    {
        public void Execute()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                SceneSetup.LoadScene("Play");
            }
        }
    }

    public class LevelRestartSystem : IReactiveSystem, IEnsureComponents
    {
        public TriggerOnEvent trigger { get { return GameMatcher.ActingActions.OnEntityRemoved(); } }
        public IMatcher ensureComponents { get { return Matcher.AllOf(GameMatcher.Hero, GameMatcher.Dead); } }

        public void Execute(List<Entity> entities)
        {
            var hero = entities.SingleEntity();
            if (!hero.IsActing())
            {
                SceneSetup.LoadScene("Play");
            }
        }
    }

    public class LevelLoaderSystem : IInitializeSystem, ISetPool
    {
        private Pool _pool;

        public void SetPool(Pool pool)
        {
            _pool = pool;
        }

        public void Initialize()
        {
            var levelName = PlaySetup.LevelPath;

            if (string.IsNullOrEmpty(levelName))
            {
                levelName = _pool.levels.Value.First();
                PlaySetup.LevelPath = levelName;
            }

            var level = Resources.Load("Levels/" + levelName) as TextAsset;
            var levelData = JsonLevelParser.ReadLevelData(level.text);
            LevelLoader.ReadLevelData(levelData, _pool);
        }
    }

    public class EditorTestLevelLoaderSystem : IInitializeSystem
    {
        public void Initialize()
        {
            LevelLoader.ReadLevelData(PlaySetup.EditorLevel, Pools.game);
        }
    }
}
