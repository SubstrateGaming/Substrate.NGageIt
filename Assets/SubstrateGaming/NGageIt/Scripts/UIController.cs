using Substrate.NetApi.Model.Extrinsics;
using Substrate.NetApi.Model.Types.Base;
using Substrate.Polkadot.NET.NetApiExt.Generated;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace NGageIt.Controllers
{
    public class UIController : MonoBehaviour
    {
        private UIDocument _doc;

        private VisualElement _body, _connectImg;
        private Label _blockHashTxt;

        [SerializeField]
        private VisualTreeAsset _startVisualTree;

        private VisualElement _startVE;

        private SubstrateClientExt _client;

        private uint _currentBlockNumber;

        private Dictionary<uint, string> _blockHashes;

        private bool _isRunning;

        [SerializeField]
        private BlockWorm _blockWorm;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _body = _doc.rootVisualElement.Q<VisualElement>("Body");
            _connectImg = _doc.rootVisualElement.Q<VisualElement>("ConnectImg");
            _connectImg.style.backgroundColor = Color.red;

            _blockHashTxt = _doc.rootVisualElement.Q<Label>("BlockHashTxt");

            _startVE = _startVisualTree.CloneTree();
            var startBtn = _startVE.Q<Button>("StartBtn");
            startBtn.clicked += StartBtnOnClicked;
        }

        private void Start()
        {
            AddContent(_startVE);

            _client = new SubstrateClientExt(
                new Uri("wss://rpc.polkadot.io"),
                ChargeTransactionPayment.Default());

            _blockHashes = new Dictionary<uint, string>();
            _isRunning = false;

            InvokeRepeating("UpdateInfo", 0, 1);
        }

        private void Update()
        {
            // Method intentionally left empty.
        }

        private async void UpdateInfo()
        {
            //Debug.Log("Update is called! " + DateTime.Now.ToString());

            if (_isRunning)
            {
                Debug.Log("Still running!");
                return;
            }



            if (_client != null)
            {
                if (!_client.IsConnected)
                {
                    _connectImg.style.backgroundColor = Color.red;
                    return;
                }

                _isRunning = true;

                var oldBlockNumber = _currentBlockNumber;

                var blockNumber = await _client.SystemStorage.Number(CancellationToken.None);
                _currentBlockNumber = blockNumber.Value;

                var currentGameBlockNumber = _currentBlockNumber % 64;
                var startGame = _currentBlockNumber - currentGameBlockNumber;

                if (currentGameBlockNumber == 0)
                {
                    _blockHashes.Clear();
                    _blockWorm.Reset();
                }

                if (_blockHashes.Count < currentGameBlockNumber + 1)
                {
                    for(uint i = (uint) _blockHashes.Count; i < currentGameBlockNumber + 1; i++)
                    {
                        if (!_blockHashes.ContainsKey(i))
                        {
                            var key = startGame + i;
                            var blockNr = new BlockNumber();
                            blockNr.Create(startGame + i);
                            var blockHash = await _client.Chain.GetBlockHashAsync(blockNr, CancellationToken.None);
                            if (blockHash != null && blockHash.Value != null)
                            {
                                _blockHashes.Add(i, blockHash.Value);
                                Debug.Log(key + " block hash " + blockHash.Value);
                                _blockWorm.AddBlock(blockHash.Value);
                            }
                            else
                            {
                                Debug.Log(key + " block hash is null");
                            }
                        }
                    }
                }

                if (oldBlockNumber != _currentBlockNumber)
                {
                    Debug.Log("Game is on block " + currentGameBlockNumber + " of 64!");

                    var blockHash = await _client.Chain.GetBlockHashAsync(CancellationToken.None);
                    if (blockHash != null)
                    {
                        _blockHashTxt.text = blockHash.Value;
                    }
                }
            }

            _isRunning = false;
        }

        public void AddContent(VisualElement content)
        {
            _body.Clear();
            _body.Add(content);
            content.StretchToParentSize();
        }

        private async void StartBtnOnClicked()
        {
            _body.Clear();
            _connectImg.style.backgroundColor = Color.yellow;
            await _client.ConnectAsync();

            if (_client.IsConnected)
            {
                _connectImg.style.backgroundColor = Color.green;
            }
            else
            {
                _connectImg.style.backgroundColor = Color.red;
            }
        }

        //IEnumerator Connect()
        //{
        //    return YieldInstruction();
        //}
    }
}