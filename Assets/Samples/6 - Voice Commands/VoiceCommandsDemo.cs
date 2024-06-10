using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VoiceCommandsDemo : MonoBehaviour
{
    public VoiceCommandsManager VC;

    public delegate void thenDoDelegate(string command);

    public Text resultText;

    // Start is called before the first frame update
    void Start()
    {
        RegisterCommands();
        
    }

    public void RegisterCommands()
    {
        VC.RegisterCommand("Try saying this!", (thenDoDelegate)thenDoA);
        VC.RegisterCommand("Here's my choice.", (thenDoDelegate)thenDoB);
        VC.RegisterCommand("Where am I?", (thenDoDelegate)thenDoC);
    }

    public void thenDoA(string command)
    {
        resultText.text += "You said \""+command+"\"\n";

        VC?.UnregisterAll();//Unregistering is also important to clear transcription. 

        RegisterCommands();
    }

    public void thenDoB(string command)
    {
        resultText.text += "You said \"" + command + "\"\n";

        VC?.UnregisterAll();//Unregistering is also important to clear transcription. 

        VC.RegisterCommand("I'll have some eggs and bacon", (thenDoDelegate)thenDoA);
        VC.RegisterCommand("Not hungry, thanks.", (thenDoDelegate)thenDoA);
    }

    public void thenDoC(string command)
    {
        resultText.text += "You said \"" + command + "\"\n";

        VC?.UnregisterAll();//Unregistering is also important to clear transcription. 

        //capitalizaiton and punctuation are totally forgiving...
        VC.RegisterCommand("oh i remember now...    I'm SITTING at my desk!", (thenDoDelegate)thenDoA);
    }
}
