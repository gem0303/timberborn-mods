using Bindito.Core;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;
using Timberborn.WorkSystem;

namespace Local.IdleWorkerAlert
{
    [Context("Game")]
    public sealed class IdleWorkerAlertConfigurator : Configurator
    {
        protected override void Configure()
        {
            Bind<IdleWorkerSettings>().AsSingleton();
            Bind<IdleWorkerMonitor>().AsTransient();
            Bind<IdleWorkerFragment>().AsSingleton();

            MultiBind<TemplateModule>()
                .ToProvider(ProvideTemplateModule)
                .AsSingleton();

            MultiBind<EntityPanelModule>()
                .ToProvider<EntityPanelModuleProvider>()
                .AsSingleton();
        }

        private static TemplateModule ProvideTemplateModule()
        {
            TemplateModule.Builder builder = new TemplateModule.Builder();
            builder.AddDecorator<Workplace, IdleWorkerMonitor>();
            return builder.Build();
        }

        private sealed class EntityPanelModuleProvider : IProvider<EntityPanelModule>
        {
            private readonly IdleWorkerFragment _fragment;

            public EntityPanelModuleProvider(IdleWorkerFragment fragment)
            {
                _fragment = fragment;
            }

            public EntityPanelModule Get()
            {
                EntityPanelModule.Builder builder = new EntityPanelModule.Builder();
                builder.AddTopFragment(_fragment, 10);
                return builder.Build();
            }
        }
    }

    [Context("MainMenu")]
    public sealed class IdleWorkerAlertMainMenuConfigurator : Configurator
    {
        protected override void Configure()
        {
            Bind<IdleWorkerSettings>().AsSingleton();
        }
    }
}
