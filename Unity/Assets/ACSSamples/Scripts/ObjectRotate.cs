// Copyright (c) 2023 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using UnityEngine;

public class ObjectRotate : MonoBehaviour
{
    private void Update()
    {
        transform.Rotate(10.0f * Time.deltaTime, 10.0f * Time.deltaTime, -7.0f * Time.deltaTime);
    }
}