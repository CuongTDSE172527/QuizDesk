using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
namespace QuizDesk.Mobile;
public partial class MainView {
    void LibrarySelectKey(object sender,KeyEventArgs args){
        if(currentPage!="bank"||busy||active||studying||currentDialog!=null||args.Key!=Key.A||args.KeyModifiers is not (KeyModifiers.Control or KeyModifiers.Meta))return;
        if(args.Source is Visual visual&&(visual is TextBox||visual.GetVisualAncestors().Any(a=>a is TextBox)))return;
        if(!selectingBanks){if(!PersistBank())return;selectingBanks=true;expandedBankId=null;foreach(var item in libraryItems)selectedBanks.Add(item.Id);Navigate("bank");}
        else foreach(var check in librarySelections.Values)check.IsChecked=true;
        args.Handled=true;
    }
}
