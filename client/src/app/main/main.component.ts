import { Component, OnInit } from '@angular/core';
import { CurrentUserService } from '../current-user.service';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {
  isLoggedIn: boolean = false;

  constructor(
    private currentUser: CurrentUserService) { }

  ngOnInit(): void {
    this.currentUser.isLoggedIn$.subscribe(isLoggedIn => {
      this.isLoggedIn = isLoggedIn
    })
  }

}
