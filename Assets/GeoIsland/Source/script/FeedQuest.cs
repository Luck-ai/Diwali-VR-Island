using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Parent asks for help feeding the kid. After Talk, carrying any listed food to the kid
// eats it and counts up; at the goal the counter goes away, the parent says thanks with a
// meow, and the talk UI disappears.
public class FeedQuest : MonoBehaviour
{
    public GameObject talkButton;
    public GameObject speechPanel;
    public TMP_Text speechText;
    public GameObject talkUi; // whole canvas, hidden after the thanks
    public TMP_Text kidCounter;
    public Renderer kid;
    public XRGrabInteractable[] foods;
    public int goal = 10;
    public float eatReach = .15f;
    public AudioClip eatClip;
    public AudioClip thanksClip;
    [TextArea] public string request = "my kid on the other side is hungry can you feed him something";
    public string thanks = "thanks";
    public float thanksSeconds = 3f;

    readonly HashSet<XRGrabInteractable> handled = new HashSet<XRGrabInteractable>();
    bool started, done;
    int eaten;

    void Start()
    {
        talkButton.SetActive(true);
        speechPanel.SetActive(false);
        kidCounter.gameObject.SetActive(false);
    }

    public void Talk()
    {
        if (started) return;
        started = true;
        talkButton.SetActive(false);
        speechPanel.SetActive(true);
        speechText.text = request;
        kidCounter.text = eaten + "/" + goal;
        kidCounter.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!started || done || !kid) return;
        var mouth = kid.bounds;
        mouth.Expand(eatReach * 2f);
        foreach (var food in foods)
        {
            if (!food || !food.gameObject.activeInHierarchy) continue;
            // Only food someone has picked up counts, so nothing gets eaten just by
            // starting near the kid.
            if (food.isSelected) handled.Add(food);
            if (!handled.Contains(food)) continue;
            var piece = food.GetComponentInChildren<Renderer>();
            if (!piece || !mouth.Intersects(piece.bounds)) continue;
            Eat(food);
            if (done) return;
        }
    }

    void Eat(XRGrabInteractable food)
    {
        food.gameObject.SetActive(false);
        if (eatClip) AudioSource.PlayClipAtPoint(eatClip, kid.bounds.center);
        eaten++;
        kidCounter.text = eaten + "/" + goal;
        if (eaten < goal) return;
        done = true;
        kidCounter.gameObject.SetActive(false);
        speechText.text = thanks;
        if (thanksClip) AudioSource.PlayClipAtPoint(thanksClip, transform.position);
        StartCoroutine(HideUiLater());
    }

    IEnumerator HideUiLater()
    {
        yield return new WaitForSeconds(thanksSeconds);
        talkUi.SetActive(false);
    }
}
