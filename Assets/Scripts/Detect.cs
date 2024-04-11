using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Detect : MonoBehaviour
{
    public int id;
    private TextMeshProUGUI _text;
    private Image _image;

    private void Start()
    {
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _image = GetComponent<Image>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Answer answer = other.gameObject.GetComponent<Answer>();
        if (answer)
        {
            TextMeshProUGUI textAnswer = other.GetComponentInChildren<TextMeshProUGUI>();
            Image imageAnswer = other.GetComponent<Image>();
            
            
            if (answer.id == id)
            {
                
                GameManager.Instance.PlayAudioWin();
                Debug.Log("TRue");
                //_image.color = Color.green;
                _text.text = textAnswer.text;
                imageAnswer.color = Color.green;
                StartCoroutine(NextScene());
            }
            else
            {
                if (GameManager.Instance.loser == 1)
                {
                    StartCoroutine(NextScene());
                }

                GameManager.Instance.loser++;
                GameManager.Instance.PlayAudioLose();
                Debug.Log("False");
                //_image.color = Color.red;
                imageAnswer.color = Color.red;
            }
        }
    }

    IEnumerator NextScene()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(1);
    }
}
