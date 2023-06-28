using Substrate.NetApi;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockWorm : MonoBehaviour
{
    [SerializeField]
    private Material _material;
    
    [SerializeField]
    private int secPerBlock = 12;

    [SerializeField]
    private int speed = 10;

    [SerializeField]
    private int _wormSize = 64;

    private int _wormElementsPerBlock;

    // define a list to store the blocks in the "worm"
    private readonly List<GameObject> _wormElements = new();

    private Vector3 _lastDirection = Vector3.zero;

    private readonly List<Vector3> _directionVectors = new()
    {
            Vector3.fwd,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            Vector3.down,
            Vector3.up
    };

    private int _colorIndex = 0;
    private readonly List<Color> _colors = new()
    {
            Color.blue,
            Color.green,
            Color.yellow,
            Color.red
    };

    private int _walkIndex = 0;

    private readonly float _hideAlpha = 0.025f;

    private void Awake()
    {
        _wormElementsPerBlock = Convert.ToInt32(secPerBlock * speed);
    }

    void Start()
    {
        InvokeRepeating("WalkWorm", 1f, 1f / speed);
    }

    private void WalkWorm()
    {
        Debug.Log($"Walk Worm [{_wormElements.Count}] Index {_walkIndex}.");
        
        if (_walkIndex < _wormElements.Count - _wormElementsPerBlock)
        { 
            int startWalk = _wormElements.Count > _wormSize ? _wormElements.Count - _wormSize : 0;
            for (int i = startWalk; i < _wormElements.Count  - (_wormElementsPerBlock - 1); i++)
            {
                _walkIndex = i;
                Color colorShow = _wormElements[_walkIndex].GetComponent<Renderer>().material.color;
                colorShow.a = 1f;
                _wormElements[_walkIndex].GetComponent<Renderer>().material.color = colorShow;
            }
        }
        else if(_wormElements.Count > _walkIndex)
        {
            Color colorShow = _wormElements[_walkIndex].GetComponent<Renderer>().material.color;
            colorShow.a = 1f;
            _wormElements[_walkIndex].GetComponent<Renderer>().material.color = colorShow;
            _walkIndex++;

            if (_walkIndex > _wormSize)
            {
                Color colorHidde = _wormElements[_walkIndex - _wormSize].GetComponent<Renderer>().material.color;
                colorHidde.a = _hideAlpha;
                _wormElements[_walkIndex - _wormSize].GetComponent<Renderer>().material.color = colorHidde;
            }
        }
    }
    // create a function to add a new block to the "worm"
    public void AddBlock(string blockHash)
    {
        _colorIndex++;
        var colorIndexA = _colors[_colorIndex % 4];
        var colorIndexB = _colors[(_colorIndex + 1) % 4];
        var byteArray = Utils.HexToByteArray(blockHash);
        for (int i = 0; i < _wormElementsPerBlock; i++)
        {
            Material wormElementMaterial = new(Shader.Find("Standard"));
            wormElementMaterial.CopyPropertiesFromMaterial(_material);
            Color newColor = Color.Lerp(colorIndexA, colorIndexB, (float) i / _wormElementsPerBlock);
            newColor.a = _hideAlpha;
            wormElementMaterial.color = newColor;
            GameObject wormElement = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wormElement.GetComponent<Renderer>().material = wormElementMaterial;

            wormElement.transform.SetParent(gameObject.transform);
            wormElement.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            if (_wormElements.Count == 0)
            {
                wormElement.transform.position = Vector3.zero;
                _lastDirection = Vector3.zero;
            }
            else
            {
                wormElement.transform.position = _wormElements.Last().transform.position;
                var direction = GetNextDirection(_lastDirection, wormElement.transform.position, byteArray[i]);
                wormElement.transform.position += direction;
                //Debug.Log(wormElement.transform.position.ToString());
                _lastDirection = direction;
            }

            // add the new block to the "worm" in the direction calculated
            _wormElements.Add(wormElement);

            if (_wormElements.Count > _wormSize + (2 * _wormElementsPerBlock))
            {
                Destroy(_wormElements[0]);
                _wormElements.RemoveAt(0);
                if (_walkIndex > 0)
                {
                    _walkIndex--;
                }
            }
        }
    }

    private Vector3 GetNextDirection(Vector3 direction, Vector3 lastPosition, byte blockHashByte)
    {
        var directions = _directionVectors.Where(p => p != -direction).ToList();
        var legitDirections = directions.Where(p => !_wormElements.Any(q => q.transform.position == lastPosition + p) 
        && (lastPosition + p).x < 10 
        && (lastPosition + p).x > -10 
        && (lastPosition + p).y < 10 
        && (lastPosition + p).y > -10 
        && (lastPosition + p).z < 10 
        && (lastPosition + p).z > -10).ToList();

        if (legitDirections.Count == 0)
        {
            Debug.Log("Worm locked!");
            return Vector3.zero;
        }

        return legitDirections[blockHashByte % legitDirections.Count];
    }

    internal void Reset()
    {
        _wormElements.ForEach(p => Destroy(p));
        _wormElements.Clear();
        _lastDirection = Vector3.zero;
        _walkIndex = 0;
    }
}