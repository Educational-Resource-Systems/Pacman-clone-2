<?php
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: POST, GET, OPTIONS");
header("Access-Control-Allow-Headers: Content-Type");

$servername = "localhost";
$username = "ersdev_pacman";
$password = "fish007";
$dbname = "ersdev_pacmangame";

try {
    $conn = new mysqli($servername, $username, $password, $dbname);

    if ($conn->connect_error) {
        http_response_code(500);
        echo "Error: Database connection failed: " . $conn->connect_error;
        exit;
    }

    if ($_SERVER["REQUEST_METHOD"] == "POST") {
        if (!isset($_POST['player_name'], $_POST['email'], $_POST['score'])) {
            http_response_code(400);
            echo "Error: Missing required fields";
            exit;
        }

        $player_name = $_POST['player_name'];
        $email = $_POST['email'];
        $score = (int)$_POST['score'];
        $date = date("Y-m-d H:i:s");

        // Check for duplicate score
        $checkSql = "SELECT COUNT(*) as count FROM scores WHERE player_name = ? AND email = ? AND score = ? AND date = ?";
        $checkStmt = $conn->prepare($checkSql);
        $checkStmt->bind_param("ssis", $player_name, $email, $score, $date);
        $checkStmt->execute();
        $result = $checkStmt->get_result();
        $row = $result->fetch_assoc();

        if ($row['count'] > 0) {
            echo "Score already exists";
            $checkStmt->close();
            $conn->close();
            exit;
        }
        $checkStmt->close();

        // Insert score
        $stmt = $conn->prepare("INSERT INTO scores (player_name, email, score, date) VALUES (?, ?, ?, ?)");
        $stmt->bind_param("ssis", $player_name, $email, $score, $date);

        if ($stmt->execute()) {
            echo "Score saved successfully";
        } else {
            http_response_code(500);
            echo "Error: Query failed: " . $stmt->error;
        }
        $stmt->close();
    } else {
        $sql = "SELECT player_name, score FROM scores ORDER BY score DESC LIMIT 10";
        $result = $conn->query($sql);

        if (!$result) {
            http_response_code(500);
            echo "Error: Query failed: " . $conn->error;
            exit;
        }

        echo "name\tscore\n";
        if ($result->num_rows > 0) {
            while ($row = $result->fetch_assoc()) {
                $name = str_replace("\t", "\\\t", $row['player_name']);
                echo $name . "\t" . (int)$row['score'] . "\n";
            }
        }
    }

    $conn->close();
} catch (Exception $e) {
    http_response_code(500);
    echo "Error: Server error: " . $e->getMessage();
}
?>