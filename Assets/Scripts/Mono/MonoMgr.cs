using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MonoMgr : BaseManager<MonoMgr>
{
    private MonoScript monoScript;
    public MonoMgr()
    {
        monoScript = new GameObject("MonoScript").AddComponent<MonoScript>();
    }
    public void AddUpadateListener(UnityAction updateMethod)
    {
        monoScript.AddUpadateListener(updateMethod);
    }
    public void RemoveUpadateListener(UnityAction updateMethod)
    {
        monoScript.RemoveUpadateListener(updateMethod);
    }
    public Coroutine StartCoroutine(string methodName)
    {
        return monoScript.StartCoroutine(methodName);
    }
    public Coroutine StartCoroutine(IEnumerator routine)
    {
        return monoScript.StartCoroutine(routine);
    }
    public Coroutine StartCoroutine(string methodName, object value)
    {
        return monoScript.StartCoroutine(methodName, value);
    }
    public void StopAllCoroutines()
    {
        monoScript.StopAllCoroutines();
    }
    public void StopCoroutine(IEnumerator routine)
    {
        monoScript.StopCoroutine(routine);
    }
    public void StopCoroutine(Coroutine routine)
    {
        monoScript.StopCoroutine(routine);
    }
    public void StopCoroutine(string methodName)
    {
        monoScript.StopCoroutine(methodName);
    }
}
