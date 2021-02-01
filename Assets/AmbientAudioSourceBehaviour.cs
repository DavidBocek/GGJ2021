using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbientAudioSourceBehaviour : MonoBehaviour
{
    public float fadeInTime;
    public float fadeOutTime;

    private AudioSource m_audioSrc;

    // Start is called before the first frame update
    void Start()
    {
        m_audioSrc = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter( Collider other )
    {
        if ( other.CompareTag( "Player" ) )
        {
            m_audioSrc.volume = 0;
            m_audioSrc.Play();
            StartCoroutine( "FadeIn" );

            foreach( GameObject obj in GameObject.FindGameObjectsWithTag( "AmbientAudioSource" ) )
            {
                if ( obj == this.gameObject )
                    continue;

                AmbientAudioSourceBehaviour otherAmbSrc = obj.GetComponent<AmbientAudioSourceBehaviour>();

                otherAmbSrc.StartCoroutine( "FadeOut" );
            }
        }
    }

    IEnumerator FadeIn()
    {
        float timeElapsed = 0.0f;

        while ( timeElapsed < fadeInTime )
        {
            timeElapsed += Time.deltaTime;
            m_audioSrc.volume = Mathf.Lerp( 0, 1, timeElapsed / fadeInTime );
            yield return 0;
        }

        m_audioSrc.volume = 1.0f;
    }

    IEnumerator FadeOut()
    {
        float timeElapsed = 0.0f;

        while ( timeElapsed < fadeOutTime )
        {
            timeElapsed += Time.deltaTime;
            m_audioSrc.volume = Mathf.Lerp( 1, 0, timeElapsed / fadeOutTime );
            yield return 0;
        }

        m_audioSrc.volume = 0.0f;
    }
}
