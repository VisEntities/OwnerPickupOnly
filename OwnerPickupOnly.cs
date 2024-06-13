using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Owner Pickup Only", "VisEntities", "1.0.0")]
    [Description("Restricts entity pickup to the owner or their teammates.")]
    public class OwnerPickupOnly : RustPlugin
    {
        #region Fields

        private static OwnerPickupOnly _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Allow Teammates To Pickup")]
            public bool AllowTeammatesToPickup { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                AllowTeammatesToPickup = true,
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private object CanPickupEntity(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null)
                return null;

            if (PermissionUtil.HasPermission(player, PermissionUtil.IGNORE))
                return null;

            if (entity.OwnerID != player.userID)
            {
                if (_config.AllowTeammatesToPickup && AreTeammates(player.userID, entity.OwnerID))
                    return null;

                SendMessage(player, Lang.CannotPickupDeployable);
                return false;
            }

            return null;
        }

        #endregion Oxide Hooks

        #region Helper Functions

        public static bool AreTeammates(ulong firstPlayerId, ulong secondPlayerId)
        {
            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindPlayersTeam(firstPlayerId);
            if (team != null && team.members.Contains(secondPlayerId))
                return true;

            return false;
        }

        #endregion Helper Functions

        #region Permissions

        private static class PermissionUtil
        {
            public const string IGNORE = "ownerpickuponly.ignore";
            private static readonly List<string> _permissions = new List<string>
            {
                IGNORE,
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permissions

        #region Localization

        private class Lang
        {
            public const string CannotPickupDeployable = "CannotPickupDeployable";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.CannotPickupDeployable] = "You cannot pick up this entity as you are not the owner."
            }, this, "en");
        }

        private void SendMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}