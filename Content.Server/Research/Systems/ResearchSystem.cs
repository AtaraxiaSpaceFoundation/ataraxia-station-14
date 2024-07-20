using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Research.Systems
{
    [UsedImplicitly]
    public sealed partial class ResearchSystem : SharedResearchSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLog = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly RadioSystem _radio = default!;

        public override void Initialize()
        {
            base.Initialize();
            InitializeClient();
            InitializeConsole();
            InitializeSource();
            InitializeServer();

            SubscribeLocalEvent<TechnologyDatabaseComponent, ResearchRegistrationChangedEvent>(OnDatabaseRegistrationChanged);
        }

        /// <summary>
        /// Gets a server based on it's unique numeric id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="serverUid"></param>
        /// <param name="serverComponent"></param>
        /// <returns></returns>
        public bool TryGetServerById(int id, [NotNullWhen(true)] out EntityUid? serverUid, [NotNullWhen(true)] out ResearchServerComponent? serverComponent)
        {
            serverUid = null;
            serverComponent = null;

            var query = EntityQueryEnumerator<ResearchServerComponent>();
            while (query.MoveNext(out var uid, out var server))
            {
                if (server.Id != id)
                    continue;
                serverUid = uid;
                serverComponent = server;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets  all the servers on the grid.
        /// </summary>
        public List<ResearchServerComponent> GetAllServers(EntityUid client)
        {
            var query = EntityQueryEnumerator<ResearchServerComponent>();
            var clientTransform = Transform(client);
            var list = new List<ResearchServerComponent>(3);

            while (query.MoveNext(out var uid, out var server))
            {
                var serverTransform = Transform(uid);
                if (clientTransform.GridUid != serverTransform.GridUid)
                {
                    continue;
                }

                list.Add(server);
            }

            return list;
        }

        /// <summary>
        /// Gets the names of all the servers on the grid.
        /// </summary>
        /// <returns></returns>
        public List<string> GetServerNames(EntityUid client)
        {
            var query = EntityQueryEnumerator<ResearchServerComponent>();
            var clientTransform = Transform(client);
            var list = new List<string>(3);

            while (query.MoveNext(out var uid, out var server))
            {
                var serverTransform = Transform(uid);
                if (clientTransform.GridUid != serverTransform.GridUid)
                {
                    continue;
                }

                list.Add(server.ServerName);
            }

            return list;
        }

        /// <summary>
        /// Gets the ids of all the servers on the grid.
        /// </summary>
        /// <returns></returns>
        public List<int> GetServerIds(EntityUid client)
        {
            var query = EntityQueryEnumerator<ResearchServerComponent>();
            var clientTransform = Transform(client);
            var list = new List<int>(3);
            while (query.MoveNext(out var uid, out var server))
            {
                var serverTransform = Transform(uid);
                if (clientTransform.GridUid != serverTransform.GridUid)
                {
                    continue;
                }

                list.Add(server.Id);
            }

            return list;
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<ResearchServerComponent>();
            while (query.MoveNext(out var uid, out var server))
            {
                if (server.NextUpdateTime > _timing.CurTime)
                    continue;
                server.NextUpdateTime = _timing.CurTime + server.ResearchConsoleUpdateTime;

                UpdateServer(uid, (int) server.ResearchConsoleUpdateTime.TotalSeconds, server);
            }
        }
    }
}
