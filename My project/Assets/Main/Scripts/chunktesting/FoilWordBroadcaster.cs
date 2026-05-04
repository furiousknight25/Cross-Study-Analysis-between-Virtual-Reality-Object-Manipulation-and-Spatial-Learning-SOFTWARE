using UnityEngine;
using TMPro;

public class FoilWordBroadcaster : MonoBehaviour
{
    [Tooltip("The channel to broadcast the foil string to.")]
    public StringEventChannelSO foilSelectedChannel;
    
    [Tooltip("The string value of this foil word.")]
    public string foilWord;

    [Tooltip("The text component displaying the word on the button.")]
    public TMP_Text buttonText;

    public void SetupWord(string newWord)
    {
        foilWord = newWord;
        if (buttonText != null)
        {
            buttonText.text = newWord;
        }
    }

    public void BroadcastFoil()
    {
        if (foilSelectedChannel != null)
        {
            foilSelectedChannel.RaiseEvent(foilWord);
        }
    }
}