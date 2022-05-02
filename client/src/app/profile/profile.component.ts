import { AfterViewInit, Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CurrentUserService } from '../current-user.service';
import { DatePipe } from '@angular/common';
import { nullSafeIsEquivalent } from '@angular/compiler/src/output/output_ast';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  @ViewChildren('lastPost', { read: ElementRef })
  lastPost!: QueryList<ElementRef>;

  newsfeedPosts: any;
  page: number = 1;
  isValidFileSize: boolean = true;
  newPostContent: string = '';
  base64Image: string = "";
  // current logged in user
  user = {
    FirstName: '',
    LastName: '',
    Username: '',
    ImageSrc: '',
  };
  // current visited profile
  userInfo = {
    FirstName: '',
    LastName: '',
    EmailAddress: '',
    Birthday: 0,
    MobileNumber: '',
    Gender: ''
  }

  username: any;
  status: string | null = null;
  bio: string | null = null;
  birthday: string | null = null;
  profPic: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private currentUser: CurrentUserService) { }

  ngOnInit(): void {
    this.currentUser.currentUser$.subscribe((user) => {
      this.user = user
    })
    this.username = this.route.snapshot.paramMap.get('username');
    this.getUserProfile(this.username);

    this.getNewsfeedPosts()
  }

  gAfterViewInit(): void {
    console.log(this.newsfeedPosts);
  }

  getUserProfile(username: string) {
    const JWT = localStorage.getItem('JSONWebToken')
    if(JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        })
      };
      this.http.get<any>(`http://localhost:5000/${username}`, httpOptions)
        .subscribe((res) => {
          this.status = res.Status;
          this.bio = res.Bio;
          this.profPic = res.ImageSrc;
          this.getUserInfo(res.Status, res.OwnerId);
        })
    }
  }

  getUserInfo(status: string, ownerId: number) {
    const JWT = localStorage.getItem('JSONWebToken')
    if(JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'Status': status
        })
      };
      this.http.get<any>(`http://localhost:5000/info/${ownerId}`, httpOptions)
        .subscribe((res) => {
          this.userInfo = res;
          const pipe = new DatePipe('en-US');
          this.birthday = pipe.transform(this.userInfo.Birthday*1000, 'longDate')
        })
    }
  }

  getNewsfeedPosts() {
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT,
          'Page': JSON.stringify(this.page)
        })
      };
      this.http.get<string>("http://localhost:5000/newsfeedposts", httpOptions)
        .subscribe((newsfeedPosts) => {
          this.newsfeedPosts = newsfeedPosts
        })
    }
  }

  addFriend() {
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        }),
      };
      this.http.post<any>(`http://localhost:5000/request/${this.username}`, null, httpOptions)
        .subscribe((res) => {
          this.status = 'PendingRequest';
        })
    }
  }

  acceptRequest() {
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        }),
      };
      this.http.post<any>(`http://localhost:5000/accept/${this.username}`, null, httpOptions)
        .subscribe((res) => {
          this.status = 'Friends';
        })
    }
  }
}