﻿using Robust.Shared.Configuration;

namespace Content.Server._White;

public static class UnsafePseudoIoC // Я НАНАВИЖУ IOCMAANGERRESOLVEPOSHEL NAHUI
{
    public static IConfigurationManager ConfigurationManager = default!;

    public static void Initialize()
    {
        ConfigurationManager = IoCManager.Resolve<IConfigurationManager>();
    }

}