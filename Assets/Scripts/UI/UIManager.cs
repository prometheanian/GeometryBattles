﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeometryBattles.PlayerManager;
using GeometryBattles.BoardManager;

namespace GeometryBattles.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private int activePlayer = 0;
        
        private Board board;

        // Start is called before the first frame update
        void Start()
        {
            board = FindObjectOfType<Board>();
        }

        public Player GetActivePlayer()
        {
            if (board == null) return null;
            Player player = board.boardState.GetPlayer(activePlayer);
            if (player == null) return null;
            return player;
        }

        public Board GetBoard()
        {
            return board;
        }
    }
}