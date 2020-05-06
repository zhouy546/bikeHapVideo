using DemolitionStudios.DemolitionMedia;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediaCtr : MonoBehaviour
{

    public float speed;
    public Media media;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {

            speed = media.PlaybackSpeed;
            speed = Mathf.Clamp( speed + 0.01f,0.5f,4f);

            media.PlaybackSpeed = speed;

        }else if (Input.GetKey(KeyCode.Q))
        {
            speed = media.PlaybackSpeed;
            speed = Mathf.Clamp(speed - 0.01f, 0.5f, 4f);
               media.PlaybackSpeed = speed;

        }

        if (speed == 0.5)
        {
            if (media.IsPlaying)
            {
                media.Pause();
            }
        }else if (speed > 0.5)
        {
            if (!media.IsPlaying)
            {
                media.Play();
            }
        }

    }
}
