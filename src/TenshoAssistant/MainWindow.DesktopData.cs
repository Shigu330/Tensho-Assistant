using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// X-119 step 2, part 2 and X-151 (0.1131): the one entry the desktop application calls for the plugin's saved data, written here so that a
// change of a store breaks the build, not the application at run time (the application calls this method by its name only, checked by
// DesktopData1131Tests, instead of rebuilding the stores by reflection). The folder is the application's own copy of the plugin's folder
// (DesktopMirror), never the plugin's; the stores are built as Plugin.cs builds them, and built again when the link says their category
// changed. Categories: LocalLinkProtocol.DesktopCategories.
internal sealed partial class MainWindow
{
    internal void UseDesktopData(string folder,string category,Action<Exception,string> warn)
    {
        switch(category)
        {
            case "bank":
                var previousBank=bank;
                bank=new InventoryBankService(new PerCharacterBankStore(Path.Combine(folder,"InventoryBank"),Path.Combine(folder,"InventoryBank.json")));
                previousBank?.Dispose();
                break;
            case "craft-lists":
                CraftListStore=new Craft.CraftListStore(Path.Combine(folder,"CraftLists.json"),warn);
                break;
            case "my-tools":
                var previousTools=MyToolsStore;
                MyToolsStore=new MyTools.ToolTemplateStore(Path.Combine(folder,"MyTools.json"),warn);
                previousTools?.Dispose();
                break;
            case "voyages":
                VoyageHistorySource=new FreeCompany.VoyageHistoryStore(Path.Combine(folder,"VoyageHistory.json"),warn);
                break;
            case "beast-teams":
                var teams=Path.Combine(folder,"BeastTeams.json");
                BeastTeamsFile=()=>teams;
                beastPlans=null;beastPlansLoaded=false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category),category,"not a category the desktop application reads");
        }
    }
}
