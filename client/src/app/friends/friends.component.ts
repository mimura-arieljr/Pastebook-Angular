import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { CurrentUserService } from '../current-user.service';

export class Friend {
  constructor(
    public FirstName: string,
    public LastName: string,
    public Username: string,
    public ImageSrc: string
  ) { }
}

@Component({
  selector: 'app-friends',
  templateUrl: './friends.component.html',
  styleUrls: ['./friends.component.scss']
})
export class FriendsComponent implements OnInit {

  friend = {
    FirstName: '',
    LastName: '',
    Username: '',
    ImageSrc: '',
  };

  firstName: string = '';
  friends: Friend[] = [];

  constructor(private httpClient: HttpClient, private getCurrentUser: CurrentUserService) { }

  ngOnInit(): void {
    this.getFriend();
    this.getCurrentUser.getCurrentUser()
  }

  getFriend() {
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        })
      };
      this.httpClient.get<any>('http://localhost:5000/friends', httpOptions).subscribe(
        response => {
          console.log(response);
          this.friend = response;
          this.friends = response;
        }
      )
    }
  }

  search() {
    if(this.firstName != ""){
      this.friends = this.friends.filter(res => {
        return res.FirstName.toLocaleLowerCase().match(this.firstName.toLocaleLowerCase())
      });
    }
    else{
      this.ngOnInit();
    }
  }
}

