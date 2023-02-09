using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MonoScript : MonoBehaviour
{
    private event UnityAction updateMethod;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    void Update()
    {
        if (updateMethod != null)
            updateMethod();
    }
    public void AddUpadateListener(UnityAction updateAction)
    {
        this.updateMethod += updateAction;
    }
    public void RemoveUpadateListener(UnityAction updateAction)
    {
        this.updateMethod -= updateAction;
    }
}
