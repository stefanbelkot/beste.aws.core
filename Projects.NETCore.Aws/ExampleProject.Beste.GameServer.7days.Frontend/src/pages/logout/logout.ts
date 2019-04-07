import { Component } from '@angular/core';
import { NavController, NavParams, ModalController } from 'ionic-angular';
import { WebsocketProvider } from '../../providers/websocket-service';
import { GlobalVarsSDaysTDie } from '../../providers/globalVarsSDaysTDie';
import { SdaystdiePage } from '../sdaystdie/sdaystdie';

@Component({
  selector: 'page-logout',
  templateUrl: 'logout.html'
})
export class LogoutPage {

  webSocketProvider:WebsocketProvider = GlobalVarsSDaysTDie.webSocketProvider;
  constructor(public navCtrl: NavController, public navParams: NavParams, public modalCtrl:ModalController) {   
    this.logout();
  }

  ngOnInit() {
  }

  ngOnDestroy() {
  }
  ionViewDidLoad() {
  }

  logout(){
    GlobalVarsSDaysTDie.loggedinUser = null;
    GlobalVarsSDaysTDie.webSocketProvider.closeWebSocket();
    this.navCtrl.setRoot(SdaystdiePage, {}, {animate: true, direction: 'forward'});
  }

  //#endregion
}
