import { constantUsersPerSec, scenario, simulation } from "@gatling.io/core";
import { http, status } from "@gatling.io/http";

export default simulation((setUp) => {

    const httpProtocol = http
        .baseUrl("http://localhost:5013")
        .acceptHeader("application/json")
        .contentTypeHeader("application/json");

    const rankingScenario = scenario("Ranking/Leaderboard Load Test")
        .exec(
            http("GET /api/Scores/leaderboard")
                .get("/api/Scores/leaderboard?top=10")
                .check(status().is(200))
        )
        .pause(1)
        .exec(
            http("GET /api/Players")
                .get("/api/Players")
                .check(status().is(200))
        );

    const signalRScenario = scenario("SignalR Connection Test")
        .exec(
            http("SignalR Negotiate")
                .post("/game/negotiate?negotiateVersion=1")
                .check(status().is(200))
        );

    setUp(
        rankingScenario.injectOpen(
            constantUsersPerSec(10).during(30)
        ),
        signalRScenario.injectOpen(
            constantUsersPerSec(5).during(20)
        )
    ).protocols(httpProtocol);
});
