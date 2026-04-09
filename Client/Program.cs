using Client.Controls;
using Client.Envir;
using Client.Rendering;
using Client.Scenes;
using Library;
using Sentry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if WINDOWS
using System.Windows.Forms;
#endif

namespace Client
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ConfigReader.Load(Assembly.GetAssembly(typeof(Config)));

            if (Config.SentryEnabled && !string.IsNullOrEmpty(Config.SentryDSN))
            {
                using (SentrySdk.Init(Config.SentryDSN))
                    Init(args);
            }
            else
            {
                Init(args);
            }

            ConfigReader.Save(typeof(Config).Assembly);
        }

        static void Init(string[] args)
        {
#if WINDOWS
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);
#endif

            foreach (KeyValuePair<LibraryFile, string> pair in Libraries.LibraryList)
            {
                string path = Path.Combine(".", pair.Value);
                if (!File.Exists(path)) continue;

                CEnvir.LibraryList[pair.Key] = new MirLibrary(path);
            }
            Console.WriteLine($"[INIT] Loaded {CEnvir.LibraryList.Count} / {Libraries.LibraryList.Count} libraries");

            CEnvir.Init(args);

#if WINDOWS
            CEnvir.Target = new TargetForm();
            string requestedPipelineId = RenderingPipelineManager.NormalizePipelineId(Config.RenderingPipeline);
            if (!string.Equals(Config.RenderingPipeline, requestedPipelineId, StringComparison.OrdinalIgnoreCase))
                Config.RenderingPipeline = requestedPipelineId;
#else
            CEnvir.Target = new Platform.SDL3.SDL3GameWindow(Globals.ClientName, Config.GameSize.Width, Config.GameSize.Height);
            string requestedPipelineId = RenderingPipelineIds.SDL3OpenGL;
#endif

            string activePipelineId = RenderingPipelineManager.InitializeWithFallback(requestedPipelineId, new RenderingPipelineContext(CEnvir.Target));
            if (!string.Equals(Config.RenderingPipeline, activePipelineId, StringComparison.OrdinalIgnoreCase))
                Config.RenderingPipeline = activePipelineId;

#if WINDOWS
            DXSoundManager.Create();
#else
            var soundManager = new Audio.SDL3SoundManager();
            soundManager.Initialize(((Platform.SDL3.SDL3GameWindow)CEnvir.Target).NativeHandle);
#endif

            DXControl.ActiveScene = new LoginScene(Config.GameSize);

            RenderingPipelineManager.RunMessageLoop(CEnvir.Target, CEnvir.GameLoop);

            CEnvir.Session?.Save(true);
            CEnvir.Unload();
            RenderingPipelineManager.Shutdown();
#if WINDOWS
            DXSoundManager.Unload();
#endif
        }
    }
}
