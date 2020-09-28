﻿using IPA;
using System;
using IPA.Loader;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using SiraUtil.Zenject;
using IPA.Config.Stores;
using System.Reflection;
using System.Collections;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace SiraUtil
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; set; }
        internal static Harmony Harmony { get; set; }

        private readonly ZenjectManager _zenjectManager;

        /*[Init]
        public Plugin(IPA.Config.Config conf, Zenjector zenjector)
        {
            Config config = conf.Generated<Config>();


            // These three essentially do the same thing.
            zenjector.OnMenu<SiraMenuTestInstaller>();
            zenjector.On<MenuInstaller>().Register<SiraMenuTestInstaller>();
            zenjector.On("MenuInstaller").Register<SiraMenuTestInstaller>();

            // These three essentially do the same thing.
            zenjector.OnGame<SiraGameInstaller>().WithArguments(config);
            zenjector.On<GameCoreSceneSetup>().Register<SiraGameInstaller>().WithArguments(config);
            zenjector.On("GameCoreSceneSetup").Register<SiraGameInstaller>().WithArguments(config);

            // These two essentially do the same thing. Will install their bindings right BEFORE AppCoreInstaller
            zenjector.Before<AppCoreInstaller>().WithArguments(config);
            zenjector.Before("AppCoreInstaller").WithArguments(config);

            // This can install stuff using the MenuViewControllers's scene's SceneContext
            zenjector.On("MenuViewControllers").Register<SiraMenuTestInstaller2>().ForContext();
        }*/

        [Init]
        public Plugin(IPA.Config.Config conf, IPALogger logger)
        {
            Log = logger;
            Config config = conf.Generated<Config>();
            Harmony = new Harmony("dev.auros.sirautil");

            // Set Config Verison
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            config.MajorVersion = version.Major;
            config.MinorVersion = version.Minor;
            config.BuildVersion = version.Build;

            // Setup Zenjector
            _zenjectManager = new ZenjectManager();
            PluginInitInjector.AddInjector(typeof(Zenjector), (prev, __, meta) =>
            {
                if (prev != null) return prev;
                var zen = new Zenjector(meta.Id);
                _zenjectManager.Add(zen);
                return zen;
            });

            // Setup Own Zenject Stuff
            var zenjector = new Zenjector("SiraUtil");
            _zenjectManager.Add(zenjector);

            zenjector.OnApp<SiraInstaller>().WithParameters(config);
            zenjector.OnGame<SiraGameInstaller>();
        }

        [OnEnable]
        public void OnEnable()
        {
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            //InstallerDetector.Patch(Harmony);
            //Installer.RegisterAppInstaller(_siraInstallerInit);
            //Installer.RegisterGameCoreInstaller<SiraGameInstaller>();
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        private void SceneManager_activeSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "MenuViewControllers")
            {
                if (!SiraInstaller.ProjectContextWentOff)
                {
                    SharedCoroutineStarter.instance.StartCoroutine(BruteForceRestart());
                }
            }
        }

        private IEnumerator BruteForceRestart()
        {
            yield return new WaitForSecondsRealtime(1f);
            Resources.FindObjectsOfTypeAll<MenuTransitionsHelper>().FirstOrDefault()?.RestartGame();
        }

        [OnDisable]
        public void OnDisable()
        {
            //InstallerDetector.Unpatch(Harmony);
            Harmony.UnpatchAll();
            //Installer.UnregisterAppInstaller(_siraInstallerInit);
            //Installer.UnregisterGameCoreInstaller<SiraGameInstaller>();
            SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        }
    }
}