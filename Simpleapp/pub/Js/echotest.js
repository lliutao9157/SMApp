/*
 * echotest.js
 *
 * Derived from Echo Test of WebSocket.org (http://www.websocket.org/echo.html).
 *
 * Copyright (c) 2012 Kaazing Corporation.
 */

var url = "ws://localhost/Echo";
//ar url = "ws://192.168.31.56:8013/Echo";
var output;
var i = 1;

function init () {
  output = document.getElementById ("output");
  doWebSocket ();
}

function doWebSocket () {
  websocket = new WebSocket (url);

    websocket.onopen = function (e) {
        console.log("success");
        websocket.send(i);
    //onOpen (e);
  };

    websocket.onmessage = function (e) {

    onMessage (e);
  };

  websocket.onerror = function (e) {
    onError (e);
  };

  websocket.onclose = function (e) {
    onClose (e);
  };
}

function onOpen (event) {
  writeToScreen ("CONNECTED");
  send ("WebSocket rocks");
}

function onMessage(event) {
    i++;
    writeToScreen('<span style="color: blue;">RESPONSE: ' + event.data + '</span>');
    send(i);
  //websocket.close ();
}

function onError (event) {
  writeToScreen ('<span style="color: red;">ERROR: ' + event.data + '</span>');
}

function onClose (event) {
  writeToScreen ("DISCONNECTED");
}

function send (message) {
  writeToScreen ("SENT: " + message);
  websocket.send (message);
}

function writeToScreen (message) {
  var pre = document.createElement ("p");
  pre.style.wordWrap = "break-word";
  pre.innerHTML = message;
  output.appendChild (pre);
}

window.addEventListener ("load", init, false);