using MoonSharp.Interpreter;
using NeuroSonic;

namespace theori.Scripting
{
    public static class LuaScriptExt
    {
        public static void InitNeuroSonicEnvironment(this LuaScript script)
        {
            script["ControllerInput"] = typeof(ControllerInput);

            var tblNsc = script.NewTable();
            script["nsc"] = tblNsc;

            var tblCon = script.NewTable();
            tblNsc["controller"] = tblCon;

            tblCon["pressed"] = script.NewEvent();
            tblCon["released"] = script.NewEvent();
            tblCon["axisChanged"] = script.NewEvent();
        }

        public static void DestroyNeuroSonicEnvironment(this LuaScript script)
        {
            var controller = (Table)((Table)script["nsc"])["controller"];
            ((ScriptEvent)controller["pressed"]).Destroy();
            ((ScriptEvent)controller["released"]).Destroy();
            ((ScriptEvent)controller["axisChanged"]).Destroy();
        }

        public static void NeuroSonicControllerPressed(this LuaScript script, ControllerInput input)
        {
            var evt = (ScriptEvent)((Table)((Table)script["nsc"])["controller"])["pressed"];
            evt.Fire(input);
        }

        public static void NeuroSonicControllerReleased(this LuaScript script, ControllerInput input)
        {
            var evt = (ScriptEvent)((Table)((Table)script["nsc"])["controller"])["released"];
            evt.Fire(input);
        }

        public static void NeuroSonicControllerAxisChanged(this LuaScript script, ControllerInput input, float axisDelta)
        {
            var evt = (ScriptEvent)((Table)((Table)script["nsc"])["controller"])["axisChanged"];
            evt.Fire(input, axisDelta);
        }
    }
}
