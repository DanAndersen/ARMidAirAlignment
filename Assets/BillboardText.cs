using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BillboardText : MonoBehaviour
{
    public TMP_Text Text;
    public float DefaultFadeSeconds = 1.0f;
    public float DefaultShowSeconds = 5.0f;

    private IEnumerator coroutine;

    // Start is called before the first frame update
    void Start()
    {
        ClearText();
    }
    
    public void FadeInShow(string text)
    {
        FadeInShow(text, DefaultFadeSeconds);
    }

    public void FadeInShow(string text, float fadeSeconds)
    {
        ClearAndRunCoroutine(Coroutine_FadeInShow(text, fadeSeconds));
    }

    public void FadeInShowFadeOut(string text)
    {
        FadeInShowFadeOut(text, DefaultShowSeconds, DefaultFadeSeconds);
    }

    public void FadeInShowFadeOut(string text, float showSeconds, float fadeSeconds)
    {
        ClearAndRunCoroutine(Coroutine_FadeInShowFadeOut(text, showSeconds, fadeSeconds));
    }

    public void ClearText()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        Text.text = "";
    }

    private void ClearAndRunCoroutine(IEnumerator c)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        coroutine = c;
        StartCoroutine(coroutine);
    }

    IEnumerator Coroutine_FadeInShow(string text, float fadeSeconds)
    {
        // init
        Text.fontSharedMaterial.SetFloat("_FaceDilate", -1.0f);
        Text.text = text;

        yield return null;

        float yieldSeconds = 0.1f;

        for (float dilate = -1.0f; dilate <= 0.0f; dilate += (yieldSeconds * (1 / fadeSeconds)))
        {
            Text.fontSharedMaterial.SetFloat("_FaceDilate", dilate);

            yield return new WaitForSeconds(yieldSeconds);
        }

        Text.fontSharedMaterial.SetFloat("_FaceDilate", 0.0f);
    }

    IEnumerator Coroutine_FadeInShowFadeOut(string text, float showSeconds, float fadeSeconds)
    {
        // init
        Text.fontSharedMaterial.SetFloat("_FaceDilate", -1.0f);
        Text.text = text;

        yield return null;

        float yieldSeconds = 0.1f;

        for (float dilate = -1.0f; dilate <= 0.0f; dilate += (yieldSeconds * (1 / fadeSeconds)))
        {
            Text.fontSharedMaterial.SetFloat("_FaceDilate", dilate);

            yield return new WaitForSeconds(yieldSeconds);
        }

        Text.fontSharedMaterial.SetFloat("_FaceDilate", 0.0f);
        yield return new WaitForSeconds(showSeconds);

        for (float dilate = 0.0f; dilate >= -1.0f; dilate -= (yieldSeconds * (1 / fadeSeconds)))
        {
            Text.fontSharedMaterial.SetFloat("_FaceDilate", dilate);

            yield return new WaitForSeconds(yieldSeconds);
        }

        Text.fontSharedMaterial.SetFloat("_FaceDilate", -1.0f);
    }
}
