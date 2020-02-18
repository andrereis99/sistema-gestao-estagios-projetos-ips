var express = require('express');
var app = express();
var bodyParser = require("body-parser");
const cors = require('cors'); //needed to disable sendgrid security
const sgMail = require('@sendgrid/mail');
const EventEmitter = require('events');
var cron = require('node-cron');
const emitter = new EventEmitter();

const sql = require('mssql');

// Database configuration
var config = {
    /*database config*/
};

// Body Parser Middleware
app.use(bodyParser.json());

//CORS Middleware
app.use(function (req, res, next) {
    //Enabling CORS 
    res.header("Access-Control-Allow-Origin", "*");
    res.header("Access-Control-Allow-Methods", "GET,HEAD,OPTIONS,POST,PUT");
    res.header("Access-Control-Allow-Headers", "Origin, X-Requested-With, contentType,Content-Type, Accept, Authorization");
    next();
});

//Sockets for chat
var server = require('http').Server(app)
var io = require('socket.io')(server)

app.get('/home/chat', function (req, res) {
    res.sendFile(__dirname + '/Views/Chat_Views/Chat.cshtml')
});

server.listen(3000)
console.log('Server running...')

users = [];
connections = [];

io.sockets.on('connection', function (socket) {
    connections.push(socket);
    console.log('Connected: %s sockets connected', connections.length);

    // Disconnect
    socket.on('disconnect', function (data) {
        if (!socket.username) return;
        users.splice(users.indexOf(socket.username), 1);
        updateUsernames();
        connections.splice(connections.indexOf(socket), 1);
        console.log('Disconnected: %s sockets connected', connections.length);
    });

    // Send messages
    socket.on('send message', message => {
        socket.broadcast.emit('new message', { message: message, name: socket.username })
    })

    // New user
    socket.on('new user', function (data) {
        socket.username = data;
        users.push(socket.username);
        socket.broadcast.emit('user-connected', socket.username)
        updateUsernames();
    });

    function updateUsernames() {
        io.sockets.emit('get users', users);
    }
});

// Execute async query
async function execute(query) {

    return new Promise((resolve, reject) => {

        new sql.ConnectionPool(config).connect().then(pool => {
            return pool.request().query(query)
        }).then(result => {

            resolve(result.recordset);

            sql.close();
        }).catch(err => {

            reject(err)
            sql.close();

        });
    });
}

// Execute query with callback
var execute2 = function (res, query, callback) {
    sql.connect(config, function (err) {
        if (err) {
            console.log(err);
            res.sendStatus(500);
        }
        else {
            var request = new sql.Request();

            request.query(query, function (err, results, fields) {
                if (err) {
                    console.log("error while querying database -> " + err);
                    //res.send(err);
                }
                else {
                    callback(results, fields);
                    sql.close();
                }
            });
        }
    });
}

// Execute query with different parameters
var executeQuery = function (res, query, parameters) {
    sql.connect(config, function (err) {
        if (err) {
            console.log("there is a database connection error -> " + err);
            res.send(err);
        }
        else {
            // create request object
            var request = new sql.Request();

            // Add parameters
            parameters.forEach(function (p) {
                request.input(p.name, p.sqltype, p.value);
            });

            // query to the database
            request.query(query, function (err, result) {
                if (err) {
                    console.log("error while querying database -> " + err);
                    res.send(err);
                }
                else {
                    res.send(result);
                    sql.close();
                }
            });
        }
    });
}


// Get query that return data from notification table
app.get('/', function (req, res) {
    execute2("", 'Select * from Notification where Notification.NotificationId = (Select Max(NotificationId) From Notification)',
        function (value) {
            res.end(JSON.stringify('qlq')); // Result in JSON format
        });
})

// Get query that return data from notification table
app.get('/notifications', function (req, res) {
    execute2("", 'SELECT * FROM dbo.Notification',
        function (value) {
            res.end(JSON.stringify(value)); // Result in JSON format
        });
})

// Get query with where
app.get('/notifications/:NotificationId/', function (req, res) {
    execute('SELECT * FROM dbo.Notification WHERE NotificationId = ' + req.params.NotificationId)
        .then(function (value) {
            res.end(JSON.stringify(value)); // Result in JSON format
        });
})
/*app.post("/notifications", function(req , res){
    var parameters = [
      { name: 'Message', sqltype: sql.NVarChar, value: req.body.Message},
      { name: 'State', sqltype: sql.NVarChar,  value: req.body.state},
    ];
    var query = "INSERT INTO dbo.Notification (Message, state) VALUES (@Message, @State)";
    executeQuery (res, query, parameters)
    emitter.emit("postNotification");
    const msg = {
        to: 'my email',
        from: 'noreply@esw-ips.pt',
        subject: 'Notificacao de Sistema de Gestao de Estagios e Projetos Final de Curso',
        text: req.body.Message
    }
    const status = sgMail.send(msg).then((msg) => console.log(status));
});*/


sgMail.setApiKey('key'); //Api key for sendgrid mailer
app.use(cors());

// Notification model 
function Notification(id, message, state, addedOn, userId) {
    this.id = id;
    this.message = message;
    this.state = state;
    this.addedOn = addedOn;
    this.userId = userId;
}

// When post method is called emits a event that emails the last notification
app.post("/notifications", function (req, res) {
    execute2(res, "Select * from Notification where Notification.NotificationId = (Select Max(NotificationId) From Notification)",
        function (result) {
            if (result.length == 0) {
                res.status(500).json("Sem Notificações criadas");
            } else {
                var notification = new Notification(result.recordset[0].NotificationId, result.recordset[0].Message, result.recordset[0].state, result.recordset[0].AddedOn, result.recordset[0].userId);
                emitter.emit("postNotification", notification);
                res.send("" + notification.id + ", " + notification.message + ", " + notification.state + ", " + notification.addedOn + ", " + notification.userId);
            }
        });
});


var tableNotificationRows = 0;

// Verifica o tamanho de linhas na tabela das notificações
function callNotificationRows() {
    execute2("", "SELECT COUNT(*) as Count FROM Notification",
        function (result) {
            if (result.length == 0) {
                console.log("Sem notificações")
            } else {
                tableNotificationRows = result.recordset[0].Count;
                console.log("notification rows:" + tableNotificationRows);
            }
        }
    )
}

// Inserts current rows in the notifications table in the table rows variable
setTimeout(callNotificationRows, 1000);

//Catches the event from the cron schedule and sends the respective email
emitter.on("newNotification", function (arg) {
    execute("Select u.Email as 'Email', n.Message as 'Message', n.state as 'Estado' from [dbo].[User] as u join [dbo].[Notification] as n on u.UserId = n.UserId where n.NotificationId in (Select TOP 1 NotificationId FROM [dbo].Notification  ORDER BY NotificationId DESC)")
        .then(function (value) {
            var newData = JSON.stringify(value);
            var data = JSON.parse(newData);
            const msg = {
                to: data[0].Email,
                from: 'noreply@esw-ips.pt',
                subject: 'Notificacao de Sistema de Gestao de Estagios e Projetos Final de Curso',
                text: data[0].Message + "\n Estado: " + data[0].Estado
            }
            const status = sgMail.send(msg).then((msg) => console.log(status));
        });
    tableNotificationRows = arg;
    console.log("chegouEmmitter");
});

var tableUserRows = 0;

// Verifica o tamanho de linhas na tabela dos utilizadores
function callUserRows() {
    execute2("", "SELECT COUNT(*) as Count FROM [dbo].[User]",
        function (result) {
            if (result.length == 0) {
                console.log("Sem utilizadores")
            } else {
                console.log("entrou");
                tableUserRows = result.recordset[0].Count;
                console.log("table rows: " + tableUserRows);
            }
        }
    )
}
// Inserts current rows in the students table in the table rows variable
setTimeout(callUserRows, 6000);

function compareNotificationRows() {
    execute2("", "SELECT COUNT(*) as Count FROM Notification",
        function (result) {
            if (result.length == 0) {
                console.log("Sem notificações")
            } else {
                var tableRowsAux = result.recordset[0].Count;
                console.log(tableRowsAux);
                console.log(tableNotificationRows);
                if (tableRowsAux > tableNotificationRows) {
                    emitter.emit("newNotification", tableRowsAux);

                }

            }
        });
}

//compara as linhas da tabela dos utilizadores com as atuais.
function compareUserRows() {
    execute2("", "SELECT COUNT(*) as Count FROM [dbo].[User]",
        function (result) {
            if (result.length == 0) {
                console.log("Sem utilizadores")
            } else {
                var tableRowsAux = result.recordset[0].Count;
                console.log(tableRowsAux);
                console.log(tableUserRows);
                if (tableRowsAux > tableUserRows) {
                    DiffOldRowsNewRows = tableRowsAux - tableUserRows;
                    emitter.emit("newUser", DiffOldRowsNewRows);

                }
            }
        });
}

//Listener that gets the count of the table rows every minute, and if it's different from the beggining it emits an event.
cron.schedule("* * * * *", () => {
    compareNotificationRows();
    setTimeout(compareUserRows, 5000);

    console.log('passou 1 minuto');
});


var semaphore = 0;

//envio do email para os alunos com as credenciais
function SendEmailWithCredentials(Email, Password, StudentId) {
    const msg = {
        to: Email,
        from: 'noreply@esw-ips.pt',
        subject: 'Credenciais no sistema de gestão de estágios e projetos final de curso',
        text: "Vai por este meio as suas credenciais no sistema de gestão de estágios e projetos finais de curso para o estudante " + StudentId + ".\n E- mail: " + Email + "\n Password: " + Password
    }
    const status = sgMail.send(msg).then((msg) => console.log(status));
}

//Catches the event from the cron schedule and sends the respective email
emitter.on("newUser", function (arg) {
    execute('SELECT TOP ' + arg + ' * FROM [dbo].[User] ORDER BY UserId DESC')
        .then(function (value) {
            var newData = JSON.stringify(value);
            var data = JSON.parse(newData);
            console.log(newData);
            console.log(data);
            console.log("Tamanho data: " + data.length);
            for (var i = 0; i < data.length; i++) {
                SendEmailWithCredentials(data[i].Email, data[i].Password, data[i].StudentId);
            }
        });
    tableUserRows = arg + tableUserRows;
    console.log("chegouEmmitter");
});



//Catches the event from the post method and sends the respective email
emitter.on("postNotification", function (arg) {
    const msg = {
        to: 'my email',
        from: 'noreply@esw-ips.pt',
        subject: 'Notificacao de Sistema de Gestao de Estagios e Projetos Final de Curso',
        text: arg.message + "\n Estado: " + arg.state
    }
    const status = sgMail.send(msg).then((msg) => console.log(status));
    console.log("Foi puxada uma notificação!");
});

// Update notification from notifications table
app.put("/notifications/:NotificationId/", function (req, res) {
    var parameters = [
        { name: 'Message', sqltype: sql.NVarChar, value: req.body.Message },
        { name: 'State', sqltype: sql.NVarChar, value: req.body.state }
    ];
    var query = "UPDATE dbo.Notification SET Message = @Message , state = @State  WHERE NotificationId = " + req.params.NotificationId;
    executeQuery(res, query, parameters);
});

// Delete notification
app.delete('/notifications/:NotificationId/', function (req, res) {
    execute('DELETE FROM dbo.Notification WHERE NotificationId = ' + req.params.NotificationId)
        .then(function (value) {
            res.send("Notificação " + req.params.NotificationId + " removida");
        });
})

// Get query that return data from notification table
app.get('/students', function (req, res) {
    execute2("", 'SELECT * FROM dbo.Student',
        function (value) {
            res.end(JSON.stringify(value)); // Result in JSON format
        });
})

// Get query with where
app.get('/students/:StudentId/', function (req, res) {
    execute('SELECT * FROM dbo.Student WHERE StudentId = ' + req.params.StudentId)
        .then(function (value) {
            res.end(JSON.stringify(value)); // Result in JSON format
        });
})


// Delete notification
app.delete('/students/:StudentId/', function (req, res) {
    execute('DELETE FROM dbo.Student WHERE StudentId = ' + req.params.StudentId)
        .then(function (value) {
            res.send("Estudante " + req.params.StudentId + " removido");
        });
})