using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatBar : MonoBehaviour
{

    public Image Hp;

    public Image Hp_rad;

    public Image Power_yellow;


//血量变化方法
    public void OnHealthChange(float persentage)
    {
        Hp.fillAmount = persentage;
        
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Hp_rad.fillAmount>Hp.fillAmount)
        {
            Hp_rad.fillAmount -= Time.deltaTime;
        }
    }
}
