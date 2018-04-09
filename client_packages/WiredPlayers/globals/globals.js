let playerList = undefined;

mp.events.add('guiReady', () => {
	// Remove health regeneration
    mp.game.player.setHealthRechargeMultiplier(0.0);
	
	// Remove weapons from the vehicles
	mp.game.player.disableVehicleRewards();
	
	// Freeze the player until he logs in
	mp.players.local.freezePosition(true);
	
	mp.keys.bind(0x45, false, function() {
		// Key 'E' pressed
		if(!mp.players.local.vehicle || mp.players.local.seat > 0) {
			mp.events.callRemote('checkPlayerEventKeyStopAnim');
		}
	});

	mp.keys.bind(0x46, false, function() {
		// Key 'F' pressed
		if(!mp.players.local.vehicle) {
			// Check if player can enter any place
			mp.events.callRemote('checkPlayerEventKey');
		}
	});

	mp.keys.bind(0x4B, false, function() {
		// Key 'K' pressed	
		if(mp.players.local.vehicle && mp.players.local.seat == 0) {
			// Toggle vehicle's engine
			mp.events.callRemote('engineOnEventKey');
		}
	});
});

mp.events.add('changePlayerWalkingStyle', (player, clipSet) => {
	// Change player's walking style
	player.setMovementClipset(clipSet, 0.1);
});

mp.events.add('resetPlayerWalkingStyle', (player) => {
	// Reset player's walking style
	player.resetMovementClipset(0.0);
});

/*
NAPI.OnKeyDown.connect(function (sender, e) {
    if (e.KeyCode === Keys.F1 && playerList == null && NAPI.Data.GetEntitySharedData(NAPI.GetLocalPlayer(), "PLAYER_PLAYING") == true) {
        playerList = NAPI.CreateCefBrowser(resolution.Width * 0.4, resolution.Height * 0.8, true);
        NAPI.WaitUntilCefBrowserInit(playerList);
        NAPI.SetCefBrowserPosition(playerList, resolution.Width * 0.3, resolution.Height * 0.1);
        NAPI.LoadPageCefBrowser(playerList, "statics/html/onlinePLayerList.html");
        NAPI.WaitUntilCefBrowserLoaded(playerList);
        NAPI.SetCanOpenChat(false);
        NAPI.ShowCursor(true);
    }
});*/


/* 
NAPI.OnKeyUp.connect(function (sender, e) {
    if (!NAPI.Player.IsPlayerInAnyVehicle(NAPI.GetLocalPlayer()) && e.KeyCode === Keys.F) {
        NAPI.TriggerServerEvent("checkPlayerEventKey");
    } else if(e.KeyCode === Keys.F1 && playerList != null) {
        NAPI.DestroyCefBrowser(playerList);
        NAPI.SetCanOpenChat(true);
        NAPI.ShowCursor(false);
        playerList = null;
    } else if (e.KeyCode === Keys.E) {
        NAPI.TriggerServerEvent("checkPlayerEventKeyStopAnim");
    }
else if (e.KeyCode === Keys.F1 && playerList != null) {
        NAPI.DestroyCefBrowser(playerList);
        NAPI.SetCanOpenChat(true);
        NAPI.ShowCursor(false);
        playerList = null;
    }
});
/*
NAPI.OnUpdate.connect(function (sender, e) {
    if (playerList != null) {
        getConnectedPlayers();
    }
});

NAPI.OnResourceStop.connect(function () {
    if (playerList != null) {
        NAPI.DestroyCefBrowser(playerList);
        playerList = null;
    }
});
/*
function getConnectedPlayers() {
    if (playerList != null) {
        var playerArray = NAPI.GetWorldSharedData("SCOREBOARD");
        playerList.call("populatePlayerList", playerArray);
    }
};*/