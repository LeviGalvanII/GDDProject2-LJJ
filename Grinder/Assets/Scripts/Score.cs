using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class Clock2 : MonoBehaviour
{
    public bool playcountdown;
    public int minutes;
    public int seconds;
    public TextMeshProUGUI timer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(countdown());
    }

    // Update is called once per frame
    IEnumerator countdown()
    {
        while (playcountdown == true)
        {
            if (seconds < 60)
            {
                seconds = seconds + 1;
                yield return new WaitForSeconds(1.0f);
            }
            else
            {
                seconds = 0;
                minutes = minutes + 1;
            }

            timer.text = string.Format("{00:00}:{01:00}", minutes, seconds);

            if (minutes == 0 && seconds == 0)
            {
                playcountdown = true;
            }
        }
    }
}