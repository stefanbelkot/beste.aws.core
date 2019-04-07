import { Subject } from "rxjs/Subject";
import { WebsocketProvider } from "./websocket-service";
import { User } from "../app/classes/classes";
import { Observable } from "rxjs";
import { Output, EventEmitter } from "@angular/core";
    
export class GlobalVarsSDaysTDie {
    //public static backendUrl:string = "wss://freelancer-belkot.eu:443/ws";
    public static backendUrl:string = "wss://bestesoftware.eu:443/ws"
    //public static backendUrl:string = "ws://localhost:80/ws";
    public static tooLongInactive:boolean = false;
    public static webSocketProvider:WebsocketProvider = new WebsocketProvider();
    public static inactiveTimoutObservable = new Subject<number>();
    //public static loggedinUser:User;
    private static _loggedinUser:User;
    static get loggedinUser():User {
        return GlobalVarsSDaysTDie._loggedinUser;
    }
    static set loggedinUser(theUser:User) {
        GlobalVarsSDaysTDie._loggedinUser = theUser;
        GlobalVarsSDaysTDie.userSet.emit(GlobalVarsSDaysTDie._loggedinUser);
    }

    @Output() public static userSet: EventEmitter<User> = new EventEmitter();
}