using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  static PlayerMovement;

public class ShieldController : MonoBehaviour
{
    public GameObject shield;
    private bool isShieldActive = false;

    private void Start()
    {
        shield.SetActive(false);
    }

    public void ActivateShield()
    {
        if (!isShieldActive)
        {
            isShieldActive = true;
            shield.SetActive(true);
            StartCoroutine(DeactivateShield());
        }
    }

    private IEnumerator DeactivateShield()
    {
        yield return new WaitForSeconds(1f);
        shield.SetActive(false);
        isShieldActive = false;
    }
}
