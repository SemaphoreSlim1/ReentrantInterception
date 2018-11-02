# ReentrantInterception
This repo explores combining polly retry policies with Autofac interceptors to repeatedly call invocation targets until they succeed

Autofac, like many containers, allows for aspect-oriented programming in the form of interceptors.

Interceptors allow us to execute cross-cutting concern code before and after the target of invocation, without having to manually place it in our target method

Autofac's interceptors were built for a model where target invocations are synchronous - but what if we wanted to build something around an async model?

Maybe, we want to add some resiliency, and retry invoking the target method multiple times, invisible to the caller?

It should be easy to build an interceptor around that model right?

Some links that are inspiring this code:

https://autofaccn.readthedocs.io/en/latest/advanced/interceptors.html
https://www.hanselman.com/blog/AddingResilienceAndTransientFaultHandlingToYourNETCoreHttpClientWithPolly.aspx
http://www.primordialcode.com/blog/post/castle-dynamicproxy-dirty-trick-call-invocation-proceed-multiple-times-interceptor
http://www.thepollyproject.org
https://github.com/App-vNext/Polly


