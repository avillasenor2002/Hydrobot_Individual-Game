using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [TextArea(2, 5)]
    public string dialogueText;

    public string characterName;
    public Sprite characterIcon;

    public float textSpeed = 0.03f;       // time per letter
    public float autoHideTime = 3f;       // after line completes
}
