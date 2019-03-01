MethodCache

This library limits execution of identical anonimous funcionts in a time period. Its is very
utils to access data, like http requests ou access to cache sources.

If  you need to request the same resource a lot of times in a short period, you can use this
library.  When  you  write  a  delegate to make a request and determina an id for this, this
library  will call this delegate just one time inside a period of time. Any other request of
execution  of  this  delegate  int this time period will no be execute. Instead of this, the
system will return the same result as obtained by initial execution.



![](https://raw.githubusercontent.com/SerraFullStack/MethodCache/master/Documents/Readme.md%20resources/diagram1.png)
