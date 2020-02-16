const socket = io('http://localhost:3000')
const messageForm = document.getElementById('messageForm')
const message = document.getElementById('message')
const chat = document.getElementById('chat')
const messageArea = document.getElementById('messageArea')
const users = document.getElementById('users')
const usernameInput = document.getElementById('username-input')

const name = usernameInput.value
appendMessage('Estás online')
socket.emit('new user', name)

messageForm.addEventListener('submit', e => {
    e.preventDefault()
    appendMessage(`Tu: ` + message.value)
    socket.emit('send message', message.value)
    message.value = ''
})

socket.on('new message', function (data) {
    appendMessage(`${data.name}: ${data.message}`)
})

socket.on('get users', function (data) {
    for (i = 0; i < data.length; i++) {
        if (data[i] != name) {
            var entry = document.createElement('li');
            entry.appendChild(document.createTextNode(data[i]));
            users.appendChild(entry);
        }
    }
});

socket.on('user-connected', name => {
    appendMessage(`${name} está online`)
})

socket.on('user-disconnected', name => {
    appendMessage(`${name} ficou offline`)
})

function appendMessage(message) {
    const messageElement = document.createElement('div')
    messageElement.innerText = message
    chat.append(messageElement)
}