using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaterTank : MonoBehaviour
{
    public float maxWater = 500;
    public float currentWater;

    //DEBUG TEXT
    private string maxAsString;
    private string curAsString;
    public TextMeshProUGUI maxText;
    public TextMeshProUGUI currentText;
    public Image FillUI;

    // Start is called before the first frame update
    void Start()
    {
        currentWater = maxWater;
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate the percentage, rounding down to the nearest whole number
        int percentage = Mathf.RoundToInt((float)currentWater / maxWater * 100);

        //maxAsString = maxWater.ToString();
        curAsString = percentage.ToString();
        //maxText.text = maxAsString;
        currentText.text = curAsString + "%";

        FillUI.fillAmount = (currentWater / maxWater);

        if (Input.GetKeyDown(KeyCode.R))
        {
            currentWater = maxWater;
            Debug.Log("Water Refilled");
        }
    }
}
