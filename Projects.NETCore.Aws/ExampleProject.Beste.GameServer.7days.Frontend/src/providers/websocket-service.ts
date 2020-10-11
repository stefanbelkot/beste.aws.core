import { Injectable } from "@angular/core";
import { Subject, Observer, Observable } from 'rxjs/Rx';
import { Command } from "../app/classes/classes";
import { GlobalVarsSDaysTDie } from "./globalVarsSDaysTDie";


@Injectable()
export class WebsocketProvider {
    
    private instance:Subject<MessageEvent> = null;
    private socket:WebSocket = null;
    //private url:string = "ws://localhost:80/ws";
    private url:string = "ws://localhost:80/ws";
    //private url:string = "wss://freelancer-belkot.eu:444/ws";
    constructor() {
    }

    public getInstance ():Subject<MessageEvent>{

        if(this.instance == null){
            this.instance = this.createWebsocket();
        }
        return this.instance;
    }

    public createNewWebSocket(){
        this.closeWebSocket();
        this.instance = this.createWebsocket();
    }

    public static sendCommand(socket:Subject<any>,commandName:string, commandData:any){
        let command:Command = new Command();
        command.CommandName = commandName;
        command.CommandData = commandData;
        console.log("Sending Command: " + command.CommandName + " CommandData: " + JSON.stringify(command.CommandData));
        socket.next(command);
    }


    public closeWebSocket(){
        if (this.socket != null)
        {
            this.socket.onclose = null;
            this.instance.unsubscribe();
            this.socket.close();
            this.instance = null;
            this.socket = null; 
        }
    }

    private webSocketOnClose(event:CloseEvent){
        console.log("Websocket closed: " + event.reason);
        this.instance = null;
        this.socket = null; 
        GlobalVarsSDaysTDie.inactiveTimoutObservable.next(1);
    }
    private webSocketOnOpen(){
        console.log("Websocket opened");
    }

    private webSocketOnError(this:WebSocket, event:Event ){
        console.log("Websocket error");
    }

    public startPing(){
        setInterval(() => {
        let date = new Date().toLocaleString();
        let sock:Subject<any> = this.getInstance();
        let com:Command = new Command();
        com.CommandName = "KeepAlive";
        com.CommandData = "";
        sock.next(com);
        }, 30000);
    }


    private createWebsocket(): Subject<MessageEvent> {

        this.socket = new WebSocket(this.url);
        this.socket.onerror = this.webSocketOnError;
        this.socket.onopen = this.webSocketOnOpen;
        this.socket.onclose = this.webSocketOnClose;
        
    
        let observable = Observable.create(
                    (observer: Observer<MessageEvent>) => {
                        this.socket.onmessage = observer.next.bind(observer);
                    //WebsocketService.socket.onerror = observer.error.bind(observer);
                        return this.socket.close.bind(this.socket);
                    }
        );
        let observer = {
                next: (data: Object) => {
                    if (this.socket.readyState === WebSocket.OPEN) {
                        this.socket.send(JSON.stringify(data));
                    }
                },                       
        };
        return Subject.create(observer, observable);
    }
}