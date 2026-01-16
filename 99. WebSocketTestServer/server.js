const WebSocket = require('ws');

const socket = new WebSocket.Server({
    port: 8080
});

socket.on('connection', (ws, req) => {
    
    ws.on('message', (msg) =>{
        console.log('유저가 보낸 메세지 : ' + msg);
        ws.send('Server Echo : ' + msg);
    })

});