using System;
using System.Reflection;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.ModManagerScene;
using Timberborn.NaturalResources;
using Timberborn.TemplateInstantiation;
using UnityEngine;

namespace Local.NaturalRemovalButton
{
    /// <summary>
    /// A direct entry point called by Timberborn's mod loader. The runtime feature
    /// is registered through Bindito below; this class makes DLL startup visible
    /// in Player.log so discovery problems cannot fail silently.
    /// </summary>
    public sealed class NaturalRemovalButtonModStarter : IModStarter
    {
        public void StartMod(IModEnvironment modEnvironment)
        {
            NaturalRemovalButtonDiagnostics.Log(
                "ModStarter.StartMod invoked; runtime assembly version " +
                Assembly.GetExecutingAssembly().ImageRuntimeVersion + ".");
        }
    }

    /// <summary>
    /// Enables Timberborn's existing demolition-panel control for every natural
    /// resource blueprint. The underlying Demolishable still owns the job,
    /// progress, priority, cancellation, and persistence behavior.
    /// </summary>
    public sealed class NaturalRemovalButtonEnabler : BaseComponent, IAwakableComponent
    {
        private const string DemolishableSpecTypeName =
            "Timberborn.Demolishing.DemolishableSpec";

        private const string ShowButtonPropertyName =
            "ShowDemolishButtonInEntityPanel";

        public void Awake()
        {
            NaturalRemovalButtonDiagnostics.HelperAwoke();

            object demolishableSpec = FindDemolishableSpec();
            if (demolishableSpec == null)
            {
                NaturalRemovalButtonDiagnostics.MissingDemolishableSpec();
                return;
            }

            PropertyInfo showButtonProperty = demolishableSpec.GetType().GetProperty(
                ShowButtonPropertyName,
                BindingFlags.Instance | BindingFlags.Public);

            if (showButtonProperty == null || !showButtonProperty.CanWrite)
            {
                NaturalRemovalButtonDiagnostics.MissingWritableProperty();
                return;
            }

            if ((bool)showButtonProperty.GetValue(demolishableSpec, null))
            {
                NaturalRemovalButtonDiagnostics.ButtonAlreadyEnabled();
                return;
            }

            // The game models component specs as immutable records, but the public
            // init setter remains callable through reflection. This changes only the
            // in-memory spec loaded for the current game session; no JSON or save data
            // is modified.
            showButtonProperty.SetValue(demolishableSpec, true, null);
            NaturalRemovalButtonDiagnostics.ButtonEnabledAtRuntime();
        }

        private object FindDemolishableSpec()
        {
            foreach (object component in AllComponents)
            {
                if (component != null && component.GetType().FullName == DemolishableSpecTypeName)
                {
                    return component;
                }
            }

            return null;
        }
    }

    public sealed class NaturalRemovalButtonConfigurator : Configurator
    {
        protected override void Configure()
        {
            NaturalRemovalButtonDiagnostics.Log(
                "NaturalRemovalButtonConfigurator.Configure invoked; registering the natural-resource decorator.");

            Bind<NaturalRemovalButtonEnabler>().AsTransient();
            MultiBind<TemplateModule>()
                .ToProvider(ProvideTemplateModule)
                .AsSingleton();
        }

        private static TemplateModule ProvideTemplateModule()
        {
            NaturalRemovalButtonDiagnostics.Log(
                "TemplateModule provider invoked; mapping NaturalResourceSpec to NaturalRemovalButtonEnabler.");

            TemplateModule.Builder builder = new TemplateModule.Builder();
            builder.AddDecorator<NaturalResourceSpec, NaturalRemovalButtonEnabler>();
            return builder.Build();
        }
    }

    internal static class NaturalRemovalButtonDiagnostics
    {
        private const string Prefix = "[Natural Removal Button v0.3.0] ";

        private static int _helperAwakeCount;
        private static int _runtimeEnabledCount;
        private static bool _loggedMissingSpec;
        private static bool _loggedMissingProperty;
        private static bool _loggedAlreadyEnabled;

        public static void Log(string message)
        {
            Debug.Log(Prefix + message);
        }

        public static void HelperAwoke()
        {
            _helperAwakeCount++;
            if (ShouldLogCount(_helperAwakeCount))
            {
                Log("NaturalRemovalButtonEnabler.Awake invoked " +
                    _helperAwakeCount + " time(s).");
            }
        }

        public static void ButtonEnabledAtRuntime()
        {
            _runtimeEnabledCount++;
            if (ShouldLogCount(_runtimeEnabledCount))
            {
                Log("Enabled the entity-panel demolition button at runtime for " +
                    _runtimeEnabledCount + " natural-resource instance(s).");
            }
        }

        public static void ButtonAlreadyEnabled()
        {
            if (_loggedAlreadyEnabled)
            {
                return;
            }

            _loggedAlreadyEnabled = true;
            Log("The helper reached a natural resource whose button was already enabled " +
                "(expected for the bundled vanilla blueprint fallbacks).");
        }

        public static void MissingDemolishableSpec()
        {
            if (_loggedMissingSpec)
            {
                return;
            }

            _loggedMissingSpec = true;
            Debug.LogWarning(Prefix +
                "A decorated natural resource did not contain DemolishableSpec; it was left unchanged.");
        }

        public static void MissingWritableProperty()
        {
            if (_loggedMissingProperty)
            {
                return;
            }

            _loggedMissingProperty = true;
            Debug.LogError(Prefix +
                "DemolishableSpec no longer exposes a writable ShowDemolishButtonInEntityPanel property.");
        }

        private static bool ShouldLogCount(int count)
        {
            return count <= 3 || count == 10 || count == 50 || count == 100 || count == 500;
        }
    }
}
