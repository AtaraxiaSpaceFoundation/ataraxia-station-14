using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Ataraxia
{
    [CVarDefs]
    public sealed class AtaraxiaCCVars : CVars
    {

        /*
         * Build language
         */

        public static readonly CVarDef<string>
            ServerCulture = CVarDef.Create("ataraxia.culture", "ru-RU", CVar.REPLICATED | CVar.SERVER);

    }
}
