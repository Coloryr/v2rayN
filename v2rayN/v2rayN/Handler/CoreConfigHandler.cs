using DynamicData;
using System.IO;
using v2rayN.Mode;
using v2rayN.Resx;

namespace v2rayN.Handler
{
    /// <summary>
    /// Core configuration file processing class
    /// </summary>
    internal class CoreConfigHandler
    {
        public static int GenerateClientConfig(ProfileItem node, string? fileName, out string msg, out string content)
        {
            content = string.Empty;
            try
            {
                if (node == null)
                {
                    msg = ResUI.CheckServerSettings;
                    return -1;
                }
                var config = LazyConfig.Instance.GetConfig();

                msg = ResUI.InitialConfiguration;
                if (node.configType == EConfigType.Custom)
                {
                    return GenerateClientCustomConfig(node, fileName, out msg);
                }
                else if (config.tunModeItem.enableTun || LazyConfig.Instance.GetCoreType(node, node.configType) == ECoreType.sing_box)
                {
                    var configGenSingbox = new CoreConfigSingbox(config);
                    if (configGenSingbox.GenerateClientConfigContent(node, out SingboxConfig? singboxConfig, out msg) != 0)
                    {
                        return -1;
                    }
                    if (Utils.IsNullOrEmpty(fileName))
                    {
                        content = Utils.ToJson(singboxConfig);
                    }
                    else
                    {
                        Utils.ToJsonFile(singboxConfig, fileName, false);
                    }
                }
                else
                {
                    var coreConfigV2ray = new CoreConfigV2ray(config);
                    if (coreConfigV2ray.GenerateClientConfigContent(node, out V2rayConfig? v2rayConfig, out msg) != 0)
                    {
                        return -1;
                    }
                    if (Utils.IsNullOrEmpty(fileName))
                    {
                        content = Utils.ToJson(v2rayConfig);
                    }
                    else
                    {
                        Utils.ToJsonFile(v2rayConfig, fileName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.SaveLog("GenerateClientConfig", ex);
                msg = ResUI.FailedGenDefaultConfiguration;
                return -1;
            }
            return 0;
        }

        public static int GenerateClientConfig(List<ProfileItem> nodes, string? fileName, out string msg, out string content)
        {
            var cong = new V2rayConfig()
            {
                log = new(),
                inbounds = new(),
                outbounds = new(),
                stats = new(),
                api = new(),
                policy = new(),
                routing = new()
                {
                    domainStrategy = "AsIs",
                    rules = new()
                    { 
                        
                    }
                },
                dns = new Dns4Ray()
                {
                    servers = new()
                }
            };
            var objs = new Dictionary<int, object>();
            content = string.Empty;
            msg = "";
            int index = 1;
            try
            {
                foreach (var item in nodes)
                {
                    if (item == null)
                    {
                        msg = ResUI.CheckServerSettings;
                        return -1;
                    }
                    var config = LazyConfig.Instance.GetConfig();

                    msg = ResUI.InitialConfiguration;
                    if (item.configType == EConfigType.Custom)
                    {
                        return GenerateClientCustomConfig(item, fileName, out msg);
                    }
                    else if (config.tunModeItem.enableTun || LazyConfig.Instance.GetCoreType(item, item.configType) == ECoreType.sing_box)
                    {
                        var configGenSingbox = new CoreConfigSingbox(config);
                        if (configGenSingbox.GenerateClientConfigContent(item, out SingboxConfig? singboxConfig, out msg) != 0)
                        {
                            return -1;
                        }
                        if (Utils.IsNullOrEmpty(fileName))
                        {
                            content = Utils.ToJson(singboxConfig);
                        }
                        else
                        {
                            objs.Add(objs.Count, singboxConfig);
                            //Utils.ToJsonFile(singboxConfig, fileName, false);
                        }
                    }
                    else
                    {
                        var coreConfigV2ray = new CoreConfigV2ray(config);
                        if (coreConfigV2ray.GenerateClientConfigContent(item, out V2rayConfig? v2rayConfig, out msg) != 0)
                        {
                            return -1;
                        }
                        if (Utils.IsNullOrEmpty(fileName))
                        {
                            content = Utils.ToJson(v2rayConfig);
                        }
                        else
                        {
                            if (v2rayConfig != null)
                            {
                                cong.log.loglevel = v2rayConfig.log.loglevel;
                                cong.log.access = v2rayConfig.log.access;
                                cong.log.error = v2rayConfig.log.error;

                                foreach (var item1 in v2rayConfig.inbounds)
                                {
                                    if (cong.inbounds.Any(item2 => item1.tag == item2.tag))
                                    {
                                        continue;
                                    }

                                    cong.inbounds.Add(item1);
                                }

                                foreach (var item1 in v2rayConfig.outbounds)
                                {
                                    if (item1.tag == "proxy")
                                    {
                                        item1.tag += $"-{index++}";
                                        cong.outbounds.Add(item1);
                                        continue;
                                    }
                                    else if (!cong.outbounds.Any(item2 => item1.tag == item2.tag))
                                    {
                                        cong.outbounds.Add(item1);
                                    }
                                }
                                if (v2rayConfig.dns is Dns4Ray dns)
                                {
                                    var dns1 = (cong.dns as Dns4Ray)!;
                                    foreach (var item1 in dns.servers)
                                    {
                                        if (!dns1.servers.Contains(item1))
                                        {
                                            dns1.servers.Add(item1);
                                        }
                                    }
                                }
                            }
                            //objs.Add(v2rayConfig);
                            //Utils.ToJsonFile(v2rayConfig, fileName, false);
                        }
                    }

                }

                objs.Add(objs.Count, cong);
                Utils.ToJsonFile(objs, fileName, false);
            }
            catch (Exception ex)
            {
                Utils.SaveLog("GenerateClientConfig", ex);
                msg = ResUI.FailedGenDefaultConfiguration;
                return -1;
            }
            return 0;
        }

        private static int GenerateClientCustomConfig(ProfileItem node, string? fileName, out string msg)
        {
            try
            {
                if (node == null || fileName is null)
                {
                    msg = ResUI.CheckServerSettings;
                    return -1;
                }

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                string addressFileName = node.address;
                if (!File.Exists(addressFileName))
                {
                    addressFileName = Utils.GetConfigPath(addressFileName);
                }
                if (!File.Exists(addressFileName))
                {
                    msg = ResUI.FailedGenDefaultConfiguration;
                    return -1;
                }
                File.Copy(addressFileName, fileName);

                //check again
                if (!File.Exists(fileName))
                {
                    msg = ResUI.FailedGenDefaultConfiguration;
                    return -1;
                }

                //overwrite port
                if (node.preSocksPort <= 0)
                {
                    var fileContent = File.ReadAllLines(fileName).ToList();
                    var coreType = LazyConfig.Instance.GetCoreType(node, node.configType);
                    switch (coreType)
                    {
                        case ECoreType.v2fly:
                        case ECoreType.SagerNet:
                        case ECoreType.Xray:
                        case ECoreType.v2fly_v5:
                            break;

                        case ECoreType.clash:
                        case ECoreType.clash_meta:
                            //remove the original
                            var indexPort = fileContent.FindIndex(t => t.Contains("port:"));
                            if (indexPort >= 0)
                            {
                                fileContent.RemoveAt(indexPort);
                            }
                            indexPort = fileContent.FindIndex(t => t.Contains("socks-port:"));
                            if (indexPort >= 0)
                            {
                                fileContent.RemoveAt(indexPort);
                            }

                            fileContent.Add($"port: {LazyConfig.Instance.GetLocalPort(Global.InboundHttp)}");
                            fileContent.Add($"socks-port: {LazyConfig.Instance.GetLocalPort(Global.InboundSocks)}");
                            break;
                    }
                    File.WriteAllLines(fileName, fileContent);
                }

                msg = string.Format(ResUI.SuccessfulConfiguration, "");
            }
            catch (Exception ex)
            {
                Utils.SaveLog("GenerateClientCustomConfig", ex);
                msg = ResUI.FailedGenDefaultConfiguration;
                return -1;
            }
            return 0;
        }

        public static string GenerateClientSpeedtestConfigString(Config config, List<ServerTestItem> selecteds, out string msg)
        {
            var coreConfigV2ray = new CoreConfigV2ray(config);
            return coreConfigV2ray.GenerateClientSpeedtestConfigString(selecteds, out msg);
        }
    }
}